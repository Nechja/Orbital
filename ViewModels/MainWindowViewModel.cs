using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Services;
using OrbitalDocking.ViewModels.Pages;

namespace OrbitalDocking.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    private readonly IDockerService _docker;
    private readonly IPreferencesStore _preferences;
    private readonly IUpdateChecker _updates;

    public MainWindowViewModel(
        IDockerService docker,
        IPreferencesStore preferences,
        IUpdateChecker updates,
        AppState state,
        ContainersPageViewModel containers,
        ImagesPageViewModel images,
        VolumesPageViewModel volumes,
        NetworksPageViewModel networks,
        LogsPageViewModel logs,
        SettingsPageViewModel settings)
    {
        _docker = docker;
        _preferences = preferences;
        _updates = updates;
        var saved = preferences.Load();
        _isSidebarCollapsed = saved.SidebarCollapsed;
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

        _ = ConnectAsync();
        _ = Task.Run(() => _docker.StartMonitoringEvents());

        foreach (var page in Pages)
            page.Activate();

        if (saved.CheckForUpdatesOnStartup)
            _ = CheckForUpdatesAsync();
    }

    public async Task CheckForUpdatesAsync()
    {
        State.LatestUpdate = await _updates.CheckAsync();
    }

    public AppState State { get; }
    public ContainersPageViewModel Containers { get; }
    public ImagesPageViewModel Images { get; }
    public VolumesPageViewModel Volumes { get; }
    public NetworksPageViewModel Networks { get; }
    public LogsPageViewModel Logs { get; }
    public SettingsPageViewModel Settings { get; }

    public IReadOnlyList<PageViewModel> Pages { get; }

    [ObservableProperty]
    private PageViewModel _currentPage;

    [ObservableProperty]
    private PageViewModel? _selectedNavPage;

    public bool IsSettingsActive => CurrentPage == Settings;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    [RelayCommand]
    private void ToggleSidebar() => IsSidebarCollapsed = !IsSidebarCollapsed;

    partial void OnIsSidebarCollapsedChanged(bool value) =>
        _preferences.Save(_preferences.Load() with { SidebarCollapsed = value });

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
