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
/// A page that lists one kind of Docker resource (images, volumes, networks) with text search.
/// Subclasses supply how to load items and how search matches them.
/// </summary>
public abstract partial class ResourcePageViewModel<TItem> : DockerPageViewModel
{
    private readonly TimeSpan _refreshInterval;
    private IReadOnlyList<TItem> _all = Array.Empty<TItem>();
    private bool _started;

    protected ResourcePageViewModel(IDockerService docker, AppState state, IUiDispatcher ui, TimeSpan refreshInterval)
        : base(docker, state, ui)
    {
        _refreshInterval = refreshInterval;
    }

    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>Items matching the current search, in load order.</summary>
    public ObservableCollection<TItem> Items { get; } = new();

    protected abstract Task<ErrorOr<IReadOnlyList<TItem>>> LoadAsync();

    protected abstract bool Matches(TItem item, string search);

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

    private void ApplyFilter()
    {
        Items.Clear();
        var search = SearchText.Trim();
        foreach (var item in string.IsNullOrEmpty(search) ? _all : _all.Where(i => Matches(i, search)))
            Items.Add(item);
    }

    protected static bool ContainsIgnoreCase(string? text, string search) =>
        text?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
}
