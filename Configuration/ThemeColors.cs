namespace OrbitalDocking.Configuration;

public static class ThemeColors
{
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
    }

    public static class HighContrastDark
    {
        // Primary colors - brighter, more saturated
        public const string Primary = "#00FFFF";        // Bright cyan
        public const string PrimaryDark = "#00CCCC";
        public const string Accent = "#FF00FF";         // Bright magenta

        // Background colors - pure black for maximum contrast
        public const string Background = "#000000";
        public const string BackgroundSecondary = "#0A0A0A";
        public const string BackgroundTertiary = "#1A1A1A";
        public const string Surface = "#000000";
        public const string Card = "#1A1A1A";

        // Text colors - pure white and high contrast grays
        public const string TextPrimary = "#FFFFFF";
        public const string TextSecondary = "#CCCCCC";
        public const string TextTertiary = "#999999";
        public const string TextMuted = "#666666";

        // State colors - vivid, highly saturated
        public const string Success = "#00FF00";        // Bright green
        public const string Warning = "#FFFF00";        // Bright yellow
        public const string Error = "#FF0000";          // Bright red
        public const string Info = "#0080FF";           // Bright blue

        // Container state colors
        public const string ContainerRunning = "#00FF00";
        public const string ContainerPaused = "#FFFF00";
        public const string ContainerStopped = "#808080";
        public const string ContainerRestarting = "#00FFAA";
        public const string ContainerDead = "#FF0000";
        public const string ContainerCreated = "#808080";

        // Border colors - high visibility
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
    }

    public static class Soft
    {
        // Primary colors - soft pastels
        public const string Primary = "#A8DADC";        // Soft cyan/aqua
        public const string PrimaryDark = "#89C2C4";
        public const string Accent = "#E0B0FF";         // Soft lavender

        // Background colors - warm, muted tones
        public const string Background = "#F8F4F0";     // Warm off-white
        public const string BackgroundSecondary = "#FFF8F5";
        public const string BackgroundTertiary = "#FFF0E8";
        public const string Surface = "#FFFAF7";
        public const string Card = "#FFFFFF";

        // Text colors - soft, not harsh
        public const string TextPrimary = "#3D3D3D";
        public const string TextSecondary = "#7A7A7A";
        public const string TextTertiary = "#A8A8A8";
        public const string TextMuted = "#C8C8C8";

        // State colors - muted pastels
        public const string Success = "#A3D9A5";        // Soft green
        public const string Warning = "#FFD4A3";        // Soft peach
        public const string Error = "#F4A5A5";          // Soft coral
        public const string Info = "#B0C4DE";           // Soft blue

        // Container state colors
        public const string ContainerRunning = "#A3D9A5";
        public const string ContainerPaused = "#FFD4A3";
        public const string ContainerStopped = "#B8B8B8";
        public const string ContainerRestarting = "#C5E8C5";
        public const string ContainerDead = "#F4A5A5";
        public const string ContainerCreated = "#B8B8B8";

        // Border colors - very subtle
        public const string Border = "#E8E0D8";
        public const string BorderLight = "#F0E8E0";
        public const string BorderFocus = "#A8DADC";

        // Semantic action colors - soft but distinguishable
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
        public const string NetworkAccent = "#F5E6B3";  // Soft gold
        public const string LogsText = "#5A5A5A";
        public const string LogsTimestamp = "#9A9A9A";

        // Docker status
        public const string DockerOnline = "#A3D9A5";
        public const string DockerOffline = "#F4A5A5";
        public const string DockerConnecting = "#FFD4A3";
    }

    // Helper method to get theme colors based on mode
    // Usage: var colors = isDark ? ThemeColors.Dark : ThemeColors.Light;
    // This is handled in the ViewModels directly
}