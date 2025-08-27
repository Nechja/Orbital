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
        
        // Docker status
        public const string DockerOnline = "#10B981";
        public const string DockerOffline = "#EF4444";
        public const string DockerConnecting = "#F59E0B";
    }
    
    // Helper method to get theme colors based on mode
    // Usage: var colors = isDark ? ThemeColors.Dark : ThemeColors.Light;
    // This is handled in the ViewModels directly
}