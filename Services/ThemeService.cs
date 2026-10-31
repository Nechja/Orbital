using System;
using Avalonia;
using OrbitalDocking.Models;
using OrbitalDocking.Themes;

namespace OrbitalDocking.Services;

/// <summary>
/// Switches the application's theme variant. Colors live in Themes/Palette.axaml and follow the
/// variant automatically through DynamicResource.
/// </summary>
public sealed class ThemeService : IThemeService
{
    private readonly Application _application;

    public ThemeService()
    {
        _application = Application.Current ?? throw new InvalidOperationException("Application not initialized");
        _application.RequestedThemeVariant = OrbitalThemes.For(CurrentTheme);
    }

    public ThemeMode CurrentTheme { get; private set; } = ThemeMode.Dark;

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public void SetTheme(ThemeMode theme)
    {
        if (CurrentTheme == theme)
            return;

        var previous = CurrentTheme;
        CurrentTheme = theme;
        _application.RequestedThemeVariant = OrbitalThemes.For(theme);
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previous, theme));
    }
}
