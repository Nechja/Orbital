using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public class ThemeService : IThemeService
{
    private ThemeMode _currentTheme = ThemeMode.Dark;
    private readonly Application _application;

    public ThemeService()
    {
        _application = Application.Current ?? throw new InvalidOperationException("Application not initialized");
        InitializeTheme();
    }

    public ThemeMode CurrentTheme => _currentTheme;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public Task SetThemeAsync(ThemeMode theme)
    {
        if (_currentTheme == theme)
            return Task.CompletedTask;

        var oldTheme = _currentTheme;
        _currentTheme = theme;

        ApplyTheme(theme);
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(oldTheme, theme));

        return Task.CompletedTask;
    }

    public async Task ToggleThemeAsync()
    {
        var newTheme = _currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        await SetThemeAsync(newTheme);
    }

    private void InitializeTheme()
    {
        ApplyTheme(_currentTheme);
    }

    private void ApplyTheme(ThemeMode theme)
    {
        if (_application.Styles[0] is not FluentTheme fluentTheme)
        {
            fluentTheme = new FluentTheme();
            _application.Styles[0] = fluentTheme;
        }

        switch (theme)
        {
            case ThemeMode.Dark:
                _application.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case ThemeMode.Light:
                _application.RequestedThemeVariant = ThemeVariant.Light;
                break;
            case ThemeMode.System:
                _application.RequestedThemeVariant = ThemeVariant.Default;
                break;
        }

        UpdateCustomColors(theme);
    }

    private void UpdateCustomColors(ThemeMode theme)
    {
        var resources = _application.Resources;
        
        if (theme == ThemeMode.Dark)
        {
            resources["PrimaryColor"] = Color.Parse("#5E72E4");
            resources["AccentColor"] = Color.Parse("#825EE4");
            resources["BackgroundColor"] = Color.Parse("#0B0E1A");
            resources["SurfaceColor"] = Color.Parse("#151823");
            resources["CardColor"] = Color.Parse("#1A1D2E");
            resources["TextPrimaryColor"] = Color.Parse("#E8EAED");
            resources["TextSecondaryColor"] = Color.Parse("#9CA3AF");
            resources["BorderColor"] = Color.Parse("#2A2E3F");
            resources["SuccessColor"] = Color.Parse("#10B981");
            resources["WarningColor"] = Color.Parse("#F59E0B");
            resources["ErrorColor"] = Color.Parse("#EF4444");
        }
        else
        {
            resources["PrimaryColor"] = Color.Parse("#5E72E4");
            resources["AccentColor"] = Color.Parse("#825EE4");
            resources["BackgroundColor"] = Color.Parse("#F8F9FA");
            resources["SurfaceColor"] = Color.Parse("#FFFFFF");
            resources["CardColor"] = Color.Parse("#FFFFFF");
            resources["TextPrimaryColor"] = Color.Parse("#1F2937");
            resources["TextSecondaryColor"] = Color.Parse("#6B7280");
            resources["BorderColor"] = Color.Parse("#E5E7EB");
            resources["SuccessColor"] = Color.Parse("#10B981");
            resources["WarningColor"] = Color.Parse("#F59E0B");
            resources["ErrorColor"] = Color.Parse("#EF4444");
        }
    }
}