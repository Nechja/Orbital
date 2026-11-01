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
    private readonly ILogger<SettingsPageViewModel> _logger;

    public SettingsPageViewModel(IThemeService themes, IDockerService docker, AppState state, ILogger<SettingsPageViewModel> logger)
    {
        _themes = themes;
        _docker = docker;
        _logger = logger;
        State = state;
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
