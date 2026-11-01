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

    public ObservableCollection<TItem> Items { get; } = new();

    public ObservableCollection<TItem> SelectedItems { get; } = new();

    [ObservableProperty]
    private int _selectedCount;

    protected abstract string Noun { get; }
    protected abstract string DefaultSortColumn { get; }
    protected abstract Task<ErrorOr<IReadOnlyList<TItem>>> LoadAsync();
    protected abstract bool Matches(TItem item, string search);
    protected abstract IComparable? SortKey(TItem item, string column);
    protected abstract string Describe(TItem item);
    protected abstract Task<ErrorOr<Success>> RemoveAsync(TItem item);
    protected abstract Task<ErrorOr<Success>> PruneAsync();

    partial void OnSearchTextChanged(string value) => ApplyFilter();

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
