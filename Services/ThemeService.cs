using System;
using Avalonia;
using OrbitalDocking.Models;
using OrbitalDocking.Themes;

namespace OrbitalDocking.Services;

public sealed class ThemeService : IThemeService
{
    private readonly Application _application;
    private readonly IPreferencesStore _preferences;

    public ThemeService(IPreferencesStore preferences)
    {
        _application = Application.Current ?? throw new InvalidOperationException("Application not initialized");
        _preferences = preferences;
        CurrentTheme = preferences.Load().Theme;
        _application.RequestedThemeVariant = OrbitalThemes.For(CurrentTheme);
    }

    public ThemeMode CurrentTheme { get; private set; }

    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    public void SetTheme(ThemeMode theme)
    {
        if (CurrentTheme == theme)
            return;

        var previous = CurrentTheme;
        CurrentTheme = theme;
        _application.RequestedThemeVariant = OrbitalThemes.For(theme);
        _preferences.Save(_preferences.Load() with { Theme = theme });
        ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(previous, theme));
    }
}
