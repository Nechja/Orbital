using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OrbitalDocking.Configuration;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class SettingsPageViewModel : PageViewModel
{
    private readonly IThemeService _themes;
    private readonly IDockerService _docker;
    private readonly IUpdateChecker _updates;
    private readonly IPreferencesStore _preferences;
    private readonly ILogger<SettingsPageViewModel> _logger;

    public SettingsPageViewModel(
        IThemeService themes,
        IDockerService docker,
        IUpdateChecker updates,
        IPreferencesStore preferences,
        AppState state,
        ILogger<SettingsPageViewModel> logger)
    {
        _themes = themes;
        _docker = docker;
        _updates = updates;
        _preferences = preferences;
        _logger = logger;
        State = state;
        _checkForUpdatesOnStartup = preferences.Load().CheckForUpdatesOnStartup;
    }

    public string CurrentVersion => _updates.CurrentVersion is { Major: 0, Minor: 0, Build: <= 0 } ? "development build" : $"v{_updates.CurrentVersion}";

    [ObservableProperty]
    private bool _checkForUpdatesOnStartup;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private string _updateStatus = "Not checked yet";

    partial void OnCheckForUpdatesOnStartupChanged(bool value) =>
        _preferences.Save(_preferences.Load() with { CheckForUpdatesOnStartup = value });

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        IsCheckingForUpdates = true;
        UpdateStatus = "Checking...";
        var result = await _updates.CheckAsync();
        State.LatestUpdate = result;
        IsCheckingForUpdates = false;
        UpdateStatus = result switch
        {
            { Error: { } error } => $"Check failed: {error}",
            { IsUpdateAvailable: true, Latest: { } latest } => $"v{latest} is available",
            { IsDevelopmentBuild: true, Latest: { } latest } => $"Latest release is v{latest}",
            _ => "You're on the latest release",
        };
    }

    [RelayCommand]
    private void OpenRelease()
    {
        if (State.LatestUpdate?.ReleaseUrl is { } url)
            OpenUrl(url);
    }

    public override string Title => "Settings";
    public override string IconKey => "Icon.Settings";

    public AppState State { get; }

    [ObservableProperty]
    private string _dockerEndpoint = AppConstants.Docker.DefaultDockerEndpoint;

    public bool IsDarkTheme => _themes.CurrentTheme == ThemeMode.Dark;
    public bool IsLightTheme => _themes.CurrentTheme == ThemeMode.Light;
    public bool IsSoftTheme => _themes.CurrentTheme == ThemeMode.Soft;
    public bool IsHighContrastDarkTheme => _themes.CurrentTheme == ThemeMode.HighContrastDark;
    public bool IsSystemTheme => _themes.CurrentTheme == ThemeMode.System;

    [RelayCommand]
    private void SetTheme(ThemeMode mode)
    {
        _themes.SetTheme(mode);
        State.StatusMessage = $"Theme: {mode}";
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsSoftTheme));
        OnPropertyChanged(nameof(IsHighContrastDarkTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
    }

    [RelayCommand]
    private async Task TestDockerConnectionAsync()
    {
        State.StatusMessage = "Testing Docker connection...";
        var result = await _docker.GetSystemInfoAsync();
        if (result.IsError)
        {
            State.SetDisconnected();
            State.StatusMessage = $"Connection failed: {result.FirstError.Description}";
            return;
        }

        var info = result.Value;
        State.SetConnected(info.ServerVersion);
        State.StatusMessage = $"Connected to Docker v{info.ServerVersion} - {info.Containers} containers ({info.ContainersRunning} running)";
    }

    [RelayCommand]
    private void OpenGitHub() => OpenUrl("https://github.com/Nechja/Orbital");

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open {Url}", url);
            State.StatusMessage = "Failed to open link";
        }
    }
}
