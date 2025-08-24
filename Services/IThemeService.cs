using System;
using System.Threading.Tasks;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IThemeService
{
    ThemeMode CurrentTheme { get; }
    event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    Task SetThemeAsync(ThemeMode theme);
    Task ToggleThemeAsync();
}

public record ThemeChangedEventArgs(ThemeMode OldTheme, ThemeMode NewTheme);