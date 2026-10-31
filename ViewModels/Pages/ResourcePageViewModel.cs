using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

/// <summary>
/// A page that lists one kind of Docker resource (images, volumes, networks) as a sortable,
/// searchable table with single and bulk removal. Subclasses supply loading, matching, sort keys,
/// and the remove operation.
/// </summary>
public abstract partial class ResourcePageViewModel<TItem> : DockerPageViewModel
{
    private readonly TimeSpan _refreshInterval;
    private readonly IDialogService _dialogs;
    private IReadOnlyList<TItem> _all = Array.Empty<TItem>();
    private bool _started;

    protected ResourcePageViewModel(IDockerService docker, IDialogService dialogs, AppState state, IUiDispatcher ui, TimeSpan refreshInterval)
        : base(docker, state, ui)
    {
        _dialogs = dialogs;
        _refreshInterval = refreshInterval;
        _sortColumn = DefaultSortColumn;
        SelectedItems.CollectionChanged += (_, _) => SelectedCount = SelectedItems.Count;
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _sortColumn;

    [ObservableProperty]
    private bool _sortDescending;

    /// <summary>Items matching the current search, in sort order.</summary>
    public ObservableCollection<TItem> Items { get; } = new();

    /// <summary>The list's selection; the view binds its SelectedItems to this collection.</summary>
    public ObservableCollection<TItem> SelectedItems { get; } = new();

    [ObservableProperty]
    private int _selectedCount;

    /// <summary>Singular/plural noun for status messages, e.g. "image".</summary>
    protected abstract string Noun { get; }
    protected abstract string DefaultSortColumn { get; }
    protected abstract Task<ErrorOr<IReadOnlyList<TItem>>> LoadAsync();
    protected abstract bool Matches(TItem item, string search);
    protected abstract IComparable? SortKey(TItem item, string column);
    protected abstract string Describe(TItem item);
    protected abstract Task<ErrorOr<Success>> RemoveAsync(TItem item);
    protected abstract Task<ErrorOr<Success>> PruneAsync();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    /// <summary>First activation starts periodic refresh (which refreshes immediately); later ones refresh once.</summary>
    public override void Activate()
    {
        if (_started)
        {
            _ = RefreshAsync();
            return;
        }

        _started = true;
        StartPeriodicRefresh(TimeSpan.Zero, _refreshInterval);
    }

    [RelayCommand]
    public override Task RefreshAsync() => GuardedRefreshAsync(async () =>
    {
        var result = await LoadAsync();
        if (IsDisposed) return;

        await Ui.InvokeAsync(() =>
        {
            if (result.IsError)
            {
                State.StatusMessage = $"Error: {result.FirstError.Description}";
                return;
            }

            _all = result.Value;
            Count = _all.Count;
            ApplyFilter();
        });
    });

    /// <summary>Sorts by a column; sorting by the current column again flips the direction.</summary>
    [RelayCommand]
    private void SortBy(string column)
    {
        if (column == SortColumn)
            SortDescending = !SortDescending;
        else
        {
            SortColumn = column;
            SortDescending = false;
        }
        ApplyFilter();
    }

    [RelayCommand]
    private Task RemoveItemAsync(TItem item) =>
        RunAsync($"Removing {Noun} {Describe(item)}...", $"Removed {Noun} {Describe(item)}", () => RemoveAsync(item));

    /// <summary>Removes every selected item after confirmation.</summary>
    [RelayCommand]
    private async Task RemoveSelectedAsync()
    {
        var items = SelectedItems.ToList();
        if (items.Count == 0) return;

        var what = items.Count == 1 ? $"{Noun} {Describe(items[0])}" : $"{items.Count} {Noun}s";
        if (!await _dialogs.ShowConfirmationAsync($"Remove {what}?", $"This removes the selected {Noun}{(items.Count == 1 ? "" : "s")} from Docker."))
            return;

        var failures = 0;
        foreach (var item in items)
        {
            State.StatusMessage = $"Removing {Noun} {Describe(item)}...";
            var result = await RemoveAsync(item);
            if (result.IsError) failures++;
        }

        State.StatusMessage = failures == 0
            ? $"Removed {what}"
            : $"Removed {items.Count - failures} of {items.Count} {Noun}s; {failures} failed";
        await RefreshAsync();
    }

    /// <summary>Runs Docker's prune for this resource type after confirmation.</summary>
    [RelayCommand]
    private async Task RemoveUnusedAsync()
    {
        if (!await _dialogs.ShowConfirmationAsync($"Remove unused {Noun}s?", $"Docker will remove every {Noun} that no container uses."))
            return;

        await RunAsync($"Removing unused {Noun}s...", $"Removed unused {Noun}s", PruneAsync);
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<TItem> query = string.IsNullOrEmpty(search) ? _all : _all.Where(i => Matches(i, search));
        query = SortDescending
            ? query.OrderByDescending(i => SortKey(i, SortColumn))
            : query.OrderBy(i => SortKey(i, SortColumn));

        Items.Clear();
        foreach (var item in query)
            Items.Add(item);
    }

    protected static bool ContainsIgnoreCase(string? text, string search) =>
        text?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
}
