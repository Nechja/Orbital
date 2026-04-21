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
        ApplyTheme(_currentTheme);
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

    public Task ToggleThemeAsync() =>
        SetThemeAsync(_currentTheme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark);

    private void ApplyTheme(ThemeMode theme)
    {
        if (_application.Styles[0] is not FluentTheme fluentTheme)
        {
            fluentTheme = new FluentTheme();
            _application.Styles[0] = fluentTheme;
        }

        _application.RequestedThemeVariant = theme switch
        {
            ThemeMode.Dark or ThemeMode.HighContrastDark => ThemeVariant.Dark,
            ThemeMode.Light or ThemeMode.Soft => ThemeVariant.Light,
            _ => ThemeVariant.Default,
        };

        var resources = _application.Resources;
        foreach (var (key, hex) in ThemeColors.PaletteFor(Resolve(theme)))
        {
            resources[key] = Color.Parse(hex);
        }
    }

    private ThemeMode Resolve(ThemeMode theme)
    {
        if (theme != ThemeMode.System) return theme;
        return _application.ActualThemeVariant == ThemeVariant.Light ? ThemeMode.Light : ThemeMode.Dark;
    }
}
