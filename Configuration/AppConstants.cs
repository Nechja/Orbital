using System;

namespace OrbitalDocking.Configuration;

public static class AppConstants
{
    public static class Timing
    {
        public static readonly TimeSpan EventMonitoringRetryDelay = TimeSpan.FromSeconds(5);

        public static readonly TimeSpan ContainerRefreshInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan ImageRefreshInterval = TimeSpan.FromMinutes(1);
        public static readonly TimeSpan VolumeRefreshInterval = TimeSpan.FromSeconds(30);
        public static readonly TimeSpan NetworkRefreshInterval = TimeSpan.FromSeconds(30);

        public static readonly TimeSpan StatsUpdateInterval = TimeSpan.FromSeconds(3);
        public static readonly TimeSpan StatsErrorRetryDelay = TimeSpan.FromSeconds(5);

        public static readonly int ContainerStopTimeout = 10;
        public static readonly int ContainerRestartTimeout = 10;
    }

    public static class UI
    {
        public const int DefaultWindowWidth = 1200;
        public const int DefaultWindowHeight = 700;

        public const int FadeAnimationDuration = 200;
        public const int ExpandCollapseAnimationDuration = 300;

        public const int ContainerIdDisplayLength = 12;
        public const int MaxLogLines = 1000;
    }

    public static class Docker
    {
        public const string DefaultDockerEndpoint = "unix:///var/run/docker.sock";
        public const string WindowsDockerEndpoint = "npipe://./pipe/docker_engine";

        public const string ComposeProjectLabel = "com.docker.compose.project";
        public const string ComposeServiceLabel = "com.docker.compose.service";
        public const string ComposeConfigFilesLabel = "com.docker.compose.config-files";
    }
}
