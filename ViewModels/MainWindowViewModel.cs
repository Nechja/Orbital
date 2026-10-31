using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Services;
using OrbitalDocking.ViewModels.Pages;

namespace OrbitalDocking.ViewModels;

/// <summary>
/// The shell: owns the pages, tracks which one is current, and reports Docker connectivity.
/// Everything resource-specific lives in the page view models.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDockerService _docker;

    public MainWindowViewModel(
        IDockerService docker,
        AppState state,
        ContainersPageViewModel containers,
        ImagesPageViewModel images,
        VolumesPageViewModel volumes,
        NetworksPageViewModel networks,
        LogsPageViewModel logs,
        SettingsPageViewModel settings)
    {
        _docker = docker;
        State = state;
        Containers = containers;
        Images = images;
        Volumes = volumes;
        Networks = networks;
        Logs = logs;
        Settings = settings;

        Pages = new PageViewModel[] { containers, images, volumes, networks, logs };
        _currentPage = containers;
        _selectedNavPage = containers;

        Containers.LogsRequested += (_, container) =>
        {
            CurrentPage = Logs;
            Logs.SelectedContainer = container;
        };
        Logs.BackRequested += (_, _) => CurrentPage = Containers;

        _ = ConnectAsync();
        _ = Task.Run(() => _docker.StartMonitoringEvents());

        // Start every page's periodic refresh so sidebar counts are populated without visiting each page.
        foreach (var page in Pages)
            page.Activate();
    }

    public AppState State { get; }
    public ContainersPageViewModel Containers { get; }
    public ImagesPageViewModel Images { get; }
    public VolumesPageViewModel Volumes { get; }
    public NetworksPageViewModel Networks { get; }
    public LogsPageViewModel Logs { get; }
    public SettingsPageViewModel Settings { get; }

    /// <summary>Pages listed in the sidebar, in order. Settings is reached from the sidebar footer.</summary>
    public IReadOnlyList<PageViewModel> Pages { get; }

    [ObservableProperty]
    private PageViewModel _currentPage;

    /// <summary>Sidebar selection. Null while a page outside <see cref="Pages"/> (Settings) is current.</summary>
    [ObservableProperty]
    private PageViewModel? _selectedNavPage;

    public bool IsSettingsActive => CurrentPage == Settings;

    partial void OnCurrentPageChanged(PageViewModel value)
    {
        SelectedNavPage = Pages.Contains(value) ? value : null;
        OnPropertyChanged(nameof(IsSettingsActive));

        if (value == Logs)
        {
            Logs.AvailableContainers.Clear();
            foreach (var container in Containers.Containers.OrderBy(c => c.Name))
                Logs.AvailableContainers.Add(container);
        }

        value.Activate();
    }

    partial void OnSelectedNavPageChanged(PageViewModel? value)
    {
        if (value is not null)
            CurrentPage = value;
    }

    [RelayCommand]
    private void ShowSettings() => CurrentPage = Settings;

    private async Task ConnectAsync()
    {
        var result = await _docker.GetSystemInfoAsync();
        if (result.IsError)
        {
            State.SetDisconnected();
            State.StatusMessage = result.FirstError.Description;
        }
        else
        {
            State.SetConnected(result.Value.ServerVersion);
            State.StatusMessage = "Connected to Docker";
        }
    }

    public void Dispose()
    {
        _docker.StopMonitoringEvents();
        foreach (var page in Pages)
            page.Dispose();
        Settings.Dispose();
    }
}
