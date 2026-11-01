using System;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    void SetTheme(ThemeMode theme);
}

public record ThemeChangedEventArgs(ThemeMode OldTheme, ThemeMode NewTheme);
