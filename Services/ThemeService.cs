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
            case ThemeMode.HighContrastDark:
                _application.RequestedThemeVariant = ThemeVariant.Dark;
                break;
            case ThemeMode.Light:
            case ThemeMode.Soft:
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

        if (theme == ThemeMode.Soft)
        {
            resources["PrimaryColor"] = Color.Parse(ThemeColors.Soft.Primary);
            resources["AccentColor"] = Color.Parse(ThemeColors.Soft.Accent);
            resources["BackgroundColor"] = Color.Parse(ThemeColors.Soft.Background);
            resources["SurfaceColor"] = Color.Parse(ThemeColors.Soft.Surface);
            resources["CardColor"] = Color.Parse(ThemeColors.Soft.Card);
            resources["TextPrimaryColor"] = Color.Parse(ThemeColors.Soft.TextPrimary);
            resources["TextSecondaryColor"] = Color.Parse(ThemeColors.Soft.TextSecondary);
            resources["TextTertiaryColor"] = Color.Parse(ThemeColors.Soft.TextTertiary);
            resources["BorderColor"] = Color.Parse(ThemeColors.Soft.Border);
            resources["SuccessColor"] = Color.Parse(ThemeColors.Soft.Success);
            resources["WarningColor"] = Color.Parse(ThemeColors.Soft.Warning);
            resources["ErrorColor"] = Color.Parse(ThemeColors.Soft.Error);
            resources["DangerColor"] = Color.Parse(ThemeColors.Soft.Danger);
            resources["DangerDarkColor"] = Color.Parse(ThemeColors.Soft.DangerDark);
            resources["RestartColor"] = Color.Parse(ThemeColors.Soft.Restart);
            resources["ActionAccentColor"] = Color.Parse(ThemeColors.Soft.ActionAccent);
            resources["BackgroundInputColor"] = Color.Parse(ThemeColors.Soft.BackgroundInput);
            resources["BorderInputColor"] = Color.Parse(ThemeColors.Soft.BorderInput);
            resources["TextContrastColor"] = Color.Parse(ThemeColors.Soft.TextContrast);
            resources["NetworkAccentColor"] = Color.Parse(ThemeColors.Soft.NetworkAccent);
            resources["LogsTextColor"] = Color.Parse(ThemeColors.Soft.LogsText);
            resources["LogsTimestampColor"] = Color.Parse(ThemeColors.Soft.LogsTimestamp);
        }
        else if (theme == ThemeMode.HighContrastDark)
        {
            resources["PrimaryColor"] = Color.Parse(ThemeColors.HighContrastDark.Primary);
            resources["AccentColor"] = Color.Parse(ThemeColors.HighContrastDark.Accent);
            resources["BackgroundColor"] = Color.Parse(ThemeColors.HighContrastDark.Background);
            resources["SurfaceColor"] = Color.Parse(ThemeColors.HighContrastDark.Surface);
            resources["CardColor"] = Color.Parse(ThemeColors.HighContrastDark.Card);
            resources["TextPrimaryColor"] = Color.Parse(ThemeColors.HighContrastDark.TextPrimary);
            resources["TextSecondaryColor"] = Color.Parse(ThemeColors.HighContrastDark.TextSecondary);
            resources["TextTertiaryColor"] = Color.Parse(ThemeColors.HighContrastDark.TextTertiary);
            resources["BorderColor"] = Color.Parse(ThemeColors.HighContrastDark.Border);
            resources["SuccessColor"] = Color.Parse(ThemeColors.HighContrastDark.Success);
            resources["WarningColor"] = Color.Parse(ThemeColors.HighContrastDark.Warning);
            resources["ErrorColor"] = Color.Parse(ThemeColors.HighContrastDark.Error);
            resources["DangerColor"] = Color.Parse(ThemeColors.HighContrastDark.Danger);
            resources["DangerDarkColor"] = Color.Parse(ThemeColors.HighContrastDark.DangerDark);
            resources["RestartColor"] = Color.Parse(ThemeColors.HighContrastDark.Restart);
            resources["ActionAccentColor"] = Color.Parse(ThemeColors.HighContrastDark.ActionAccent);
            resources["BackgroundInputColor"] = Color.Parse(ThemeColors.HighContrastDark.BackgroundInput);
            resources["BorderInputColor"] = Color.Parse(ThemeColors.HighContrastDark.BorderInput);
            resources["TextContrastColor"] = Color.Parse(ThemeColors.HighContrastDark.TextContrast);
            resources["NetworkAccentColor"] = Color.Parse(ThemeColors.HighContrastDark.NetworkAccent);
            resources["LogsTextColor"] = Color.Parse(ThemeColors.HighContrastDark.LogsText);
            resources["LogsTimestampColor"] = Color.Parse(ThemeColors.HighContrastDark.LogsTimestamp);
        }
        else if (theme == ThemeMode.Dark || theme == ThemeMode.System) // Default to dark for now
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
            resources["DangerColor"] = Color.Parse(ThemeColors.Dark.Danger);
            resources["DangerDarkColor"] = Color.Parse(ThemeColors.Dark.DangerDark);
            resources["RestartColor"] = Color.Parse(ThemeColors.Dark.Restart);
            resources["ActionAccentColor"] = Color.Parse(ThemeColors.Dark.ActionAccent);
            resources["BackgroundInputColor"] = Color.Parse(ThemeColors.Dark.BackgroundInput);
            resources["BorderInputColor"] = Color.Parse(ThemeColors.Dark.BorderInput);
            resources["TextContrastColor"] = Color.Parse(ThemeColors.Dark.TextContrast);
            resources["NetworkAccentColor"] = Color.Parse(ThemeColors.Dark.NetworkAccent);
            resources["LogsTextColor"] = Color.Parse(ThemeColors.Dark.LogsText);
            resources["LogsTimestampColor"] = Color.Parse(ThemeColors.Dark.LogsTimestamp);
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
            resources["DangerColor"] = Color.Parse(ThemeColors.Light.Danger);
            resources["DangerDarkColor"] = Color.Parse(ThemeColors.Light.DangerDark);
            resources["RestartColor"] = Color.Parse(ThemeColors.Light.Restart);
            resources["ActionAccentColor"] = Color.Parse(ThemeColors.Light.ActionAccent);
            resources["BackgroundInputColor"] = Color.Parse(ThemeColors.Light.BackgroundInput);
            resources["BorderInputColor"] = Color.Parse(ThemeColors.Light.BorderInput);
            resources["TextContrastColor"] = Color.Parse(ThemeColors.Light.TextContrast);
            resources["NetworkAccentColor"] = Color.Parse(ThemeColors.Light.NetworkAccent);
            resources["LogsTextColor"] = Color.Parse(ThemeColors.Light.LogsText);
            resources["LogsTimestampColor"] = Color.Parse(ThemeColors.Light.LogsTimestamp);
        }
    }
}