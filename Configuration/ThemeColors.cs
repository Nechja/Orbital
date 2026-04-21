namespace OrbitalDocking.Configuration;

public static class ThemeColors
{
    public static (string Key, string Hex)[] PaletteFor(OrbitalDocking.Models.ThemeMode mode) => mode switch
    {
        OrbitalDocking.Models.ThemeMode.Light => BuildPalette(
            Light.Primary, Light.Accent, Light.Background, Light.Surface, Light.Card,
            Light.TextPrimary, Light.TextSecondary, Light.TextTertiary, Light.Border,
            Light.Success, Light.Warning, Light.Error, Light.Danger, Light.DangerDark,
            Light.Restart, Light.ActionAccent, Light.BackgroundInput, Light.BorderInput,
            Light.TextContrast, Light.NetworkAccent, Light.LogsText, Light.LogsTimestamp,
            Light.NavigationSelected),
        OrbitalDocking.Models.ThemeMode.Soft => BuildPalette(
            Soft.Primary, Soft.Accent, Soft.Background, Soft.Surface, Soft.Card,
            Soft.TextPrimary, Soft.TextSecondary, Soft.TextTertiary, Soft.Border,
            Soft.Success, Soft.Warning, Soft.Error, Soft.Danger, Soft.DangerDark,
            Soft.Restart, Soft.ActionAccent, Soft.BackgroundInput, Soft.BorderInput,
            Soft.TextContrast, Soft.NetworkAccent, Soft.LogsText, Soft.LogsTimestamp,
            Soft.NavigationSelected),
        OrbitalDocking.Models.ThemeMode.HighContrastDark => BuildPalette(
            HighContrastDark.Primary, HighContrastDark.Accent, HighContrastDark.Background,
            HighContrastDark.Surface, HighContrastDark.Card, HighContrastDark.TextPrimary,
            HighContrastDark.TextSecondary, HighContrastDark.TextTertiary, HighContrastDark.Border,
            HighContrastDark.Success, HighContrastDark.Warning, HighContrastDark.Error,
            HighContrastDark.Danger, HighContrastDark.DangerDark, HighContrastDark.Restart,
            HighContrastDark.ActionAccent, HighContrastDark.BackgroundInput, HighContrastDark.BorderInput,
            HighContrastDark.TextContrast, HighContrastDark.NetworkAccent, HighContrastDark.LogsText,
            HighContrastDark.LogsTimestamp, HighContrastDark.NavigationSelected),
        _ => BuildPalette(
            Dark.Primary, Dark.Accent, Dark.Background, Dark.Surface, Dark.Card,
            Dark.TextPrimary, Dark.TextSecondary, Dark.TextTertiary, Dark.Border,
            Dark.Success, Dark.Warning, Dark.Error, Dark.Danger, Dark.DangerDark,
            Dark.Restart, Dark.ActionAccent, Dark.BackgroundInput, Dark.BorderInput,
            Dark.TextContrast, Dark.NetworkAccent, Dark.LogsText, Dark.LogsTimestamp,
            Dark.NavigationSelected),
    };

    private static (string Key, string Hex)[] BuildPalette(
        string primary, string accent, string background, string surface, string card,
        string textPrimary, string textSecondary, string textTertiary, string border,
        string success, string warning, string error, string danger, string dangerDark,
        string restart, string actionAccent, string backgroundInput, string borderInput,
        string textContrast, string networkAccent, string logsText, string logsTimestamp,
        string navigationSelected) =>
    [
        ("PrimaryColor", primary),
        ("AccentColor", accent),
        ("BackgroundColor", background),
        ("SurfaceColor", surface),
        ("CardColor", card),
        ("TextPrimaryColor", textPrimary),
        ("TextSecondaryColor", textSecondary),
        ("TextTertiaryColor", textTertiary),
        ("BorderColor", border),
        ("SuccessColor", success),
        ("WarningColor", warning),
        ("ErrorColor", error),
        ("DangerColor", danger),
        ("DangerDarkColor", dangerDark),
        ("RestartColor", restart),
        ("ActionAccentColor", actionAccent),
        ("BackgroundInputColor", backgroundInput),
        ("BorderInputColor", borderInput),
        ("TextContrastColor", textContrast),
        ("NetworkAccentColor", networkAccent),
        ("LogsTextColor", logsText),
        ("LogsTimestampColor", logsTimestamp),
        ("NavigationSelectedColor", navigationSelected),
    ];


    public static class Dark
    {
        // Primary colors
        public const string Primary = "#4ECDC4";
        public const string PrimaryDark = "#3BA99F";
        public const string Accent = "#825EE4";
        
        // Background colors
        public const string Background = "#050511";
        public const string BackgroundSecondary = "#0A0A1F";
        public const string BackgroundTertiary = "#0F0F2A";
        public const string Surface = "#08081A";
        public const string Card = "#1A1A3F";
        
        // Text colors
        public const string TextPrimary = "#FFFFFF";
        public const string TextSecondary = "#8888AA";
        public const string TextTertiary = "#666688";
        public const string TextMuted = "#333344";
        
        // State colors
        public const string Success = "#4ECDC4";
        public const string Warning = "#FFB347";
        public const string Error = "#FF6B6B";
        public const string Info = "#5E72E4";
        
        // Container state colors
        public const string ContainerRunning = "#4ECDC4";
        public const string ContainerPaused = "#FFB347";
        public const string ContainerStopped = "#666666";
        public const string ContainerRestarting = "#A8E6CF";
        public const string ContainerDead = "#FF6B6B";
        public const string ContainerCreated = "#666666";
        
        // Border colors
        public const string Border = "#1A1A3F";
        public const string BorderLight = "#333344";
        public const string BorderFocus = "#4ECDC4";

        // Semantic action colors
        public const string Danger = "#FF6B6B";
        public const string DangerDark = "#8B0000";
        public const string Restart = "#FFA500";
        public const string ActionAccent = "#95E1D3";

        // Input field colors
        public const string BackgroundInput = "#0F0F2A";
        public const string BorderInput = "#2A2A4F";

        // Contrast text (for colored button backgrounds)
        public const string TextContrast = "#000000";

        // Specialized colors
        public const string NetworkAccent = "#FFD700";
        public const string LogsText = "#AAAACC";
        public const string LogsTimestamp = "#6666AA";

        // Docker status
        public const string DockerOnline = "#4ECDC4";
        public const string DockerOffline = "#FF6B6B";
        public const string DockerConnecting = "#FFB347";

        // Navigation
        public const string NavigationSelected = "#E5E7EB";
    }

    public static class Light
    {
        // Primary colors
        public const string Primary = "#4ECDC4";
        public const string PrimaryDark = "#3BA99F";
        public const string Accent = "#825EE4";
        
        // Background colors
        public const string Background = "#F8F9FA";
        public const string BackgroundSecondary = "#FFFFFF";
        public const string BackgroundTertiary = "#F3F4F6";
        public const string Surface = "#FFFFFF";
        public const string Card = "#FFFFFF";
        
        // Text colors
        public const string TextPrimary = "#1F2937";
        public const string TextSecondary = "#6B7280";
        public const string TextTertiary = "#9CA3AF";
        public const string TextMuted = "#D1D5DB";
        
        // State colors
        public const string Success = "#10B981";
        public const string Warning = "#F59E0B";
        public const string Error = "#EF4444";
        public const string Info = "#3B82F6";
        
        // Container state colors
        public const string ContainerRunning = "#10B981";
        public const string ContainerPaused = "#F59E0B";
        public const string ContainerStopped = "#9CA3AF";
        public const string ContainerRestarting = "#6EE7B7";
        public const string ContainerDead = "#EF4444";
        public const string ContainerCreated = "#9CA3AF";
        
        // Border colors
        public const string Border = "#E5E7EB";
        public const string BorderLight = "#F3F4F6";
        public const string BorderFocus = "#4ECDC4";

        // Semantic action colors
        public const string Danger = "#EF4444";
        public const string DangerDark = "#991B1B";
        public const string Restart = "#F97316";
        public const string ActionAccent = "#6EE7B7";

        // Input field colors
        public const string BackgroundInput = "#F3F4F6";
        public const string BorderInput = "#D1D5DB";

        // Contrast text (for colored button backgrounds)
        public const string TextContrast = "#FFFFFF";

        // Specialized colors
        public const string NetworkAccent = "#EAB308";
        public const string LogsText = "#4B5563";
        public const string LogsTimestamp = "#9CA3AF";

        // Docker status
        public const string DockerOnline = "#10B981";
        public const string DockerOffline = "#EF4444";
        public const string DockerConnecting = "#F59E0B";

        // Navigation
        public const string NavigationSelected = "#111827";
    }

    public static class HighContrastDark
    {
        // Primary colors
        public const string Primary = "#00FFFF";
        public const string PrimaryDark = "#00CCCC";
        public const string Accent = "#FF00FF";

        // Background colors
        public const string Background = "#000000";
        public const string BackgroundSecondary = "#0A0A0A";
        public const string BackgroundTertiary = "#1A1A1A";
        public const string Surface = "#000000";
        public const string Card = "#1A1A1A";

        // Text colors
        public const string TextPrimary = "#FFFFFF";
        public const string TextSecondary = "#CCCCCC";
        public const string TextTertiary = "#999999";
        public const string TextMuted = "#666666";

        // State colors
        public const string Success = "#00FF00";
        public const string Warning = "#FFFF00";
        public const string Error = "#FF0000";
        public const string Info = "#0080FF";

        // Container state colors
        public const string ContainerRunning = "#00FF00";
        public const string ContainerPaused = "#FFFF00";
        public const string ContainerStopped = "#808080";
        public const string ContainerRestarting = "#00FFAA";
        public const string ContainerDead = "#FF0000";
        public const string ContainerCreated = "#808080";

        // Border colors
        public const string Border = "#444444";
        public const string BorderLight = "#666666";
        public const string BorderFocus = "#00FFFF";

        // Semantic action colors
        public const string Danger = "#FF0000";
        public const string DangerDark = "#CC0000";
        public const string Restart = "#FF8800";
        public const string ActionAccent = "#00FFCC";

        // Input field colors
        public const string BackgroundInput = "#1A1A1A";
        public const string BorderInput = "#666666";

        // Contrast text (for colored button backgrounds)
        public const string TextContrast = "#000000";

        // Specialized colors
        public const string NetworkAccent = "#FFFF00";
        public const string LogsText = "#FFFFFF";
        public const string LogsTimestamp = "#AAAAAA";

        // Docker status
        public const string DockerOnline = "#00FF00";
        public const string DockerOffline = "#FF0000";
        public const string DockerConnecting = "#FFFF00";

        // Navigation
        public const string NavigationSelected = "#00FFFF";
    }

    public static class Soft
    {
        // Primary colors
        public const string Primary = "#A8DADC";
        public const string PrimaryDark = "#89C2C4";
        public const string Accent = "#E0B0FF";

        // Background colors
        public const string Background = "#F8F4F0";
        public const string BackgroundSecondary = "#FFF8F5";
        public const string BackgroundTertiary = "#FFF0E8";
        public const string Surface = "#FFFAF7";
        public const string Card = "#FFFFFF";

        // Text colors
        public const string TextPrimary = "#3D3D3D";
        public const string TextSecondary = "#7A7A7A";
        public const string TextTertiary = "#A8A8A8";
        public const string TextMuted = "#C8C8C8";

        // State colors
        public const string Success = "#A3D9A5";
        public const string Warning = "#FFD4A3";
        public const string Error = "#F4A5A5";
        public const string Info = "#B0C4DE";

        // Container state colors
        public const string ContainerRunning = "#A3D9A5";
        public const string ContainerPaused = "#FFD4A3";
        public const string ContainerStopped = "#B8B8B8";
        public const string ContainerRestarting = "#C5E8C5";
        public const string ContainerDead = "#F4A5A5";
        public const string ContainerCreated = "#B8B8B8";

        // Border colors
        public const string Border = "#E8E0D8";
        public const string BorderLight = "#F0E8E0";
        public const string BorderFocus = "#A8DADC";

        // Semantic action colors
        public const string Danger = "#F4A5A5";
        public const string DangerDark = "#D88B8B";
        public const string Restart = "#FFC08A";
        public const string ActionAccent = "#B8E8E6";

        // Input field colors
        public const string BackgroundInput = "#FFF8F5";
        public const string BorderInput = "#E0D8D0";

        // Contrast text (for colored button backgrounds)
        public const string TextContrast = "#FFFFFF";

        // Specialized colors
        public const string NetworkAccent = "#F5E6B3";
        public const string LogsText = "#5A5A5A";
        public const string LogsTimestamp = "#9A9A9A";

        // Docker status
        public const string DockerOnline = "#A3D9A5";
        public const string DockerOffline = "#F4A5A5";
        public const string DockerConnecting = "#FFD4A3";

        // Navigation
        public const string NavigationSelected = "#2C3E50";
    }

}