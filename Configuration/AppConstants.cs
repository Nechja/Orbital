using System;

namespace OrbitalDocking.Configuration;

public static class AppConstants
{
    public static class Timing
    {
        // Event monitoring
        public static readonly TimeSpan EventThrottleDelay = TimeSpan.FromMilliseconds(200);
        public static readonly TimeSpan EventMonitoringRetryDelay = TimeSpan.FromSeconds(5);
        
        // Initial refresh delays
        public static readonly TimeSpan InitialLoadDelay = TimeSpan.FromMilliseconds(100);
        
        // Refresh intervals
        public static readonly TimeSpan ContainerRefreshInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ImageRefreshInterval = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan VolumeRefreshInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan NetworkRefreshInterval = TimeSpan.FromSeconds(30);
        
        // Stats monitoring
        public static readonly TimeSpan StatsUpdateInterval = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan StatsErrorRetryDelay = TimeSpan.FromSeconds(5);
        
        // Docker operations
        public static readonly int ContainerStopTimeout = 10; // seconds
        public static readonly int ContainerRestartTimeout = 10; // seconds
    }
    
    public static class UI
    {
        // Window dimensions
        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 700;
        
        // Grid spacing
        public const int MainMargin = 60;
        public const int HeaderMargin = 40;
        public const int StatusBarHeight = 30;
        
        // Animation durations (milliseconds)
        public const int FadeAnimationDuration = 200;
        public const int ExpandCollapseAnimationDuration = 300;
        
        // Text limits
        public const int ContainerIdDisplayLength = 12;
        public const int MaxLogLines = 1000;
        
        // Search
        public const int SearchDebounceMs = 300;
    }
    
    public static class Docker
    {
        // Connection
        public const string DefaultDockerEndpoint = "unix:///var/run/docker.sock";
        public const string WindowsDockerEndpoint = "npipe://./pipe/docker_engine";
        
        // Limits
        public const int MaxConcurrentOperations = 5;
        public const int MaxRetryAttempts = 3;
        
        // Labels
        public const string ComposeProjectLabel = "com.docker.compose.project";
        public const string ComposeServiceLabel = "com.docker.compose.service";
        public const string ComposeConfigFilesLabel = "com.docker.compose.config-files";
    }
    
    public static class Logging
    {
        public const string LogFileName = "orbital-docking.log";
        public const string LogDirectory = "logs";
        public const long MaxLogFileSize = 10 * 1024 * 1024; // 10MB
        public const int RetainedLogFileCount = 5;
    }
}