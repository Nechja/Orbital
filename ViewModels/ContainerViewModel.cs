using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Docker.DotNet;
using Docker.DotNet.Models;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class ContainerViewModel : ObservableObject, IDisposable
{
    private ContainerInfo _container;
    private readonly DockerClient _dockerClient;
    private CancellationTokenSource? _statsTokenSource;

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

    public ContainerViewModel(ContainerInfo container)
    {
        _container = container;
        _dockerClient = new DockerClientConfiguration().CreateClient();
    }

    public string Id => _container.Id;
    public string ShortId => _container.Id.Length > 12 ? _container.Id.Substring(0, 12) : _container.Id;
    public string Name => _container.Name;
    public string Image => _container.Image;
    public Models.ContainerState State => _container.State;
    public string Status => _container.Status;
    public DateTime Created => _container.Created;
    public string CreatedRelative => GetRelativeTime(Created);

    public bool IsRunning => State == Models.ContainerState.Running;
    public bool IsPaused => State == Models.ContainerState.Paused;
    public bool IsStopped => State == Models.ContainerState.Exited || State == Models.ContainerState.Created;

    public string StateColor => State switch
    {
        Models.ContainerState.Running => "#4ECDC4",
        Models.ContainerState.Paused => "#FFB347",
        Models.ContainerState.Exited => "#666666",
        Models.ContainerState.Dead => "#FF6B6B",
        Models.ContainerState.Restarting => "#A8E6CF",
        _ => "#666666"
    };

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

    private void StartStatsMonitoring()
    {
        StopStatsMonitoring();
        _statsTokenSource = new CancellationTokenSource();
        _ = MonitorStats(_statsTokenSource.Token);
    }

    private void StopStatsMonitoring()
    {
        _statsTokenSource?.Cancel();
        _statsTokenSource?.Dispose();
        _statsTokenSource = null;
    }

    private async Task MonitorStats(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var parameters = new ContainerStatsParameters { Stream = false };
                var progress = new Progress<ContainerStatsResponse>(stats =>
                {
                    if (stats != null)
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
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing stats for {Name}: {ex.Message}");
                        }
                    }
                });
                
                await _dockerClient.Containers.GetContainerStatsAsync(
                    Id,
                    parameters,
                    progress,
                    cancellationToken);
                
                await Task.Delay(3000, cancellationToken);
            }
            catch (Exception ex)
            {
                // Try simpler approach - just show basic info
                CpuUsage = "--";
                MemoryUsage = "--";
                NetworkIO = "--";
                DiskIO = "--";
                
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"Stats error for {Name}: {ex.Message}");
                
                await Task.Delay(5000, cancellationToken);
            }
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

    private string GetRelativeTime(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();
        
        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} days ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }
    
    public void Dispose()
    {
        StopStatsMonitoring();
        _dockerClient?.Dispose();
    }
}