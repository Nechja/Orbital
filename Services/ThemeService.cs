using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using OrbitalDocking.Configuration;
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
        
        if (theme == ThemeMode.Dark || theme == ThemeMode.System) // Default to dark for now
        {
            resources["PrimaryColor"] = Color.Parse(ThemeColors.Dark.Primary);
            resources["AccentColor"] = Color.Parse(ThemeColors.Dark.Accent);
            resources["BackgroundColor"] = Color.Parse(ThemeColors.Dark.Background);
            resources["SurfaceColor"] = Color.Parse(ThemeColors.Dark.Surface);
            resources["CardColor"] = Color.Parse(ThemeColors.Dark.Card);
            resources["TextPrimaryColor"] = Color.Parse(ThemeColors.Dark.TextPrimary);
            resources["TextSecondaryColor"] = Color.Parse(ThemeColors.Dark.TextSecondary);
            resources["TextTertiaryColor"] = Color.Parse(ThemeColors.Dark.TextTertiary);
            resources["BorderColor"] = Color.Parse(ThemeColors.Dark.Border);
            resources["SuccessColor"] = Color.Parse(ThemeColors.Dark.Success);
            resources["WarningColor"] = Color.Parse(ThemeColors.Dark.Warning);
            resources["ErrorColor"] = Color.Parse(ThemeColors.Dark.Error);
        }
        else
        {
            resources["PrimaryColor"] = Color.Parse(ThemeColors.Light.Primary);
            resources["AccentColor"] = Color.Parse(ThemeColors.Light.Accent);
            resources["BackgroundColor"] = Color.Parse(ThemeColors.Light.Background);
            resources["SurfaceColor"] = Color.Parse(ThemeColors.Light.Surface);
            resources["CardColor"] = Color.Parse(ThemeColors.Light.Card);
            resources["TextPrimaryColor"] = Color.Parse(ThemeColors.Light.TextPrimary);
            resources["TextSecondaryColor"] = Color.Parse(ThemeColors.Light.TextSecondary);
            resources["TextTertiaryColor"] = Color.Parse(ThemeColors.Light.TextTertiary);
            resources["BorderColor"] = Color.Parse(ThemeColors.Light.Border);
            resources["SuccessColor"] = Color.Parse(ThemeColors.Light.Success);
            resources["WarningColor"] = Color.Parse(ThemeColors.Light.Warning);
            resources["ErrorColor"] = Color.Parse(ThemeColors.Light.Error);
        }
    }
}