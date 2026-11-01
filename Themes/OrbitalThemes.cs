using Avalonia.Styling;
using OrbitalDocking.Models;

namespace OrbitalDocking.Themes;

/// <summary>
/// Theme variants beyond Avalonia's built-in Light/Dark. Each inherits a base variant so
/// FluentTheme control templates pick sensible defaults while Palette.axaml supplies our colors.
/// </summary>
public static class OrbitalThemes
{
    public static readonly ThemeVariant Soft = new("Soft", ThemeVariant.Light);
    public static readonly ThemeVariant HighContrastDark = new("HighContrastDark", ThemeVariant.Dark);

    public static ThemeVariant For(ThemeMode mode) => mode switch
    {
        ThemeMode.Light => ThemeVariant.Light,
        ThemeMode.Dark => ThemeVariant.Dark,
        ThemeMode.Soft => Soft,
        ThemeMode.HighContrastDark => HighContrastDark,
        _ => ThemeVariant.Default,
    };
}
