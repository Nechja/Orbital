using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Docker.DotNet;
using Docker.DotNet.Models;
using OrbitalDocking.Configuration;
using OrbitalDocking.Extensions;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

public partial class ContainerViewModel : ObservableObject, IDisposable
{
    private ContainerInfo _container;
    private CancellationTokenSource? _statsTokenSource;
    private Task? _statsMonitoringTask;
    private readonly IThemeService? _themeService;
    private readonly DockerClient? _dockerClient;
    
    public ContainerViewModel(ContainerInfo container, DockerClient? dockerClient = null, IThemeService? themeService = null)
    {
        _container = container;
        _dockerClient = dockerClient;
        _themeService = themeService;
        
        if (_themeService != null)
        {
            _themeService.ThemeChanged += OnThemeChanged;
        }
    }

    [ObservableProperty]
    private bool _isExpanded = false;

    [ObservableProperty]
    private string _cpuUsage = "--";

    [ObservableProperty]
    private string _memoryUsage = "--";

    [ObservableProperty]
    private string _networkIO = "--";

    [ObservableProperty]
    private string _diskIO = "--";

    public string Id => _container.Id;
    public string ShortId => _container.Id.Length > AppConstants.UI.ContainerIdDisplayLength 
        ? _container.Id.Substring(0, AppConstants.UI.ContainerIdDisplayLength) 
        : _container.Id;
    public string Name => _container.Name;
    public string Image => _container.Image;
    public Models.ContainerState State => _container.State;
    public string Status => _container.Status;
    public DateTime Created => _container.Created;
    public string CreatedRelative => Created.ToRelativeTime(true);
    
    public string? StackName => _container.Labels?.ContainsKey("com.docker.compose.project") == true 
        ? _container.Labels["com.docker.compose.project"] 
        : null;
    
    public string? ServiceName => _container.Labels?.ContainsKey("com.docker.compose.service") == true 
        ? _container.Labels["com.docker.compose.service"] 
        : null;
    
    public bool IsPartOfStack => !string.IsNullOrEmpty(StackName);
    
    public string StackColor => StackName.GetStackColor(StateColor);

    public bool IsRunning => State == Models.ContainerState.Running;
    public bool IsPaused => State == Models.ContainerState.Paused;
    public bool IsStopped => State == Models.ContainerState.Exited || State == Models.ContainerState.Created;
    public bool IsRestarting => State == Models.ContainerState.Restarting;
    public bool IsProblematic => State == Models.ContainerState.Restarting || State == Models.ContainerState.Dead;
    public bool CanRemove => IsStopped || IsProblematic;

    public string StateColor
    {
        get
        {
            var isDark = _themeService?.CurrentTheme != ThemeMode.Light;
            return State switch
            {
                Models.ContainerState.Running => isDark ? ThemeColors.Dark.ContainerRunning : ThemeColors.Light.ContainerRunning,
                Models.ContainerState.Paused => isDark ? ThemeColors.Dark.ContainerPaused : ThemeColors.Light.ContainerPaused,
                Models.ContainerState.Exited => isDark ? ThemeColors.Dark.ContainerStopped : ThemeColors.Light.ContainerStopped,
                Models.ContainerState.Dead => isDark ? ThemeColors.Dark.ContainerDead : ThemeColors.Light.ContainerDead,
                Models.ContainerState.Restarting => isDark ? ThemeColors.Dark.ContainerRestarting : ThemeColors.Light.ContainerRestarting,
                _ => isDark ? ThemeColors.Dark.ContainerStopped : ThemeColors.Light.ContainerStopped
            };
        }
    }

    public void UpdateFrom(ContainerInfo container)
    {
        if (container.Id != Id)
            return;

        _container = container;
        
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(StateColor));
        OnPropertyChanged(nameof(CreatedRelative));
    }

    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        
        if (IsExpanded && IsRunning)
        {
            StartStatsMonitoring();
        }
        else
        {
            StopStatsMonitoring();
        }
    }

    public void StartStatsMonitoring()
    {
        StopStatsMonitoring();
        _statsTokenSource = new CancellationTokenSource();
        _statsMonitoringTask = MonitorStats(_statsTokenSource.Token);
    }

    public void StopStatsMonitoring()
    {
        try
        {
            _statsTokenSource?.Cancel();
        }
        finally
        {
            _statsTokenSource?.Dispose();
            _statsTokenSource = null;
            _statsMonitoringTask = null;
        }
    }

    private async Task MonitorStats(CancellationToken cancellationToken)
    {
        if (_dockerClient == null)
        {
            CpuUsage = "N/A";
            MemoryUsage = "N/A";
            NetworkIO = "N/A";
            DiskIO = "N/A";
            return;
        }
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var parameters = new ContainerStatsParameters { Stream = false };
                ContainerStatsResponse? latestStats = null;
                var progress = new Progress<ContainerStatsResponse>(stats => latestStats = stats);
                
                await _dockerClient.Containers.GetContainerStatsAsync(
                    Id,
                    parameters,
                    progress,
                    cancellationToken);
                
                if (latestStats != null && !cancellationToken.IsCancellationRequested)
                {
                    UpdateStatsFromResponse(latestStats);
                }
                
                await Task.Delay(AppConstants.Timing.StatsUpdateInterval, cancellationToken);
            }
            catch
            {
                // Try simpler approach - just show basic info
                CpuUsage = "--";
                MemoryUsage = "--";
                NetworkIO = "--";
                DiskIO = "--";
                
                // Stats error - wait before retry
                await Task.Delay(AppConstants.Timing.StatsErrorRetryDelay, cancellationToken);
            }
        }
    }

    private void UpdateStatsFromResponse(ContainerStatsResponse stats)
    {
        try
        {
            // Calculate CPU percentage
            var cpuDelta = stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
            var systemDelta = stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;
            var onlineCpus = stats.CPUStats.OnlineCPUs > 0 ? stats.CPUStats.OnlineCPUs : 1;
            var cpuPercent = systemDelta > 0 ? (cpuDelta / (double)systemDelta) * onlineCpus * 100.0 : 0;
            
            CpuUsage = $"{cpuPercent:F1}%";
            
            // Calculate memory usage
            var memoryMB = stats.MemoryStats.Usage / (1024.0 * 1024.0);
            var limitMB = stats.MemoryStats.Limit / (1024.0 * 1024.0);
            var memoryPercent = limitMB > 0 ? (memoryMB / limitMB) * 100 : 0;
            MemoryUsage = $"{memoryMB:F0} MB ({memoryPercent:F0}%)";
            
            // Network stats
            if (stats.Networks != null)
            {
                long totalRx = 0, totalTx = 0;
                foreach (var network in stats.Networks.Values)
                {
                    totalRx += (long)network.RxBytes;
                    totalTx += (long)network.TxBytes;
                }
                NetworkIO = $"↓{FormatBytes(totalRx)} ↑{FormatBytes(totalTx)}";
            }
            else
            {
                NetworkIO = "↓0B ↑0B";
            }
            
            // Disk I/O stats
            if (stats.BlkioStats?.IoServiceBytesRecursive != null && stats.BlkioStats.IoServiceBytesRecursive.Count > 0)
            {
                long totalRead = 0, totalWrite = 0;
                foreach (var io in stats.BlkioStats.IoServiceBytesRecursive)
                {
                    if (io.Op == "read" || io.Op == "Read") 
                        totalRead += (long)io.Value;
                    if (io.Op == "write" || io.Op == "Write") 
                        totalWrite += (long)io.Value;
                }
                DiskIO = $"R:{FormatBytes(totalRead)} W:{FormatBytes(totalWrite)}";
            }
            else
            {
                DiskIO = "R:0B W:0B";
            }
        }
        catch
        {
            // Stats parsing error - ignore
        }
    }
    
    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:F0}{sizes[order]}";
    }

    
    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        OnPropertyChanged(nameof(StateColor));
    }
    
    public void UpdateThemeColors()
    {
        OnPropertyChanged(nameof(StateColor));
    }
    
    public void Dispose()
    {
        if (_themeService != null)
        {
            _themeService.ThemeChanged -= OnThemeChanged;
        }
        StopStatsMonitoring();
        // Don't dispose _dockerClient here - it's shared across all containers
    }
}