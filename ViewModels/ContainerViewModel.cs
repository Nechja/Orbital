using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Docker.DotNet;
using Docker.DotNet.Models;
using ContainerState = OrbitalDocking.Models.ContainerState;
using OrbitalDocking.Configuration;
using OrbitalDocking.Extensions;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class ContainerViewModel : ObservableObject, IDisposable
{
    private ContainerInfo _container;
    private CancellationTokenSource? _statsTokenSource;
    private readonly DockerClient? _dockerClient;

    public ContainerViewModel(ContainerInfo container, DockerClient? dockerClient = null)
    {
        _container = container;
        _dockerClient = dockerClient;
    }

    [ObservableProperty]
    private bool _isSelected;

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
    public ContainerState State => _container.State;
    public string Status => _container.Status;
    public DateTime Created => _container.Created;
    public string CreatedRelative => Created.ToRelativeTime(true);

    public string? StackName => _container.Labels?.GetValueOrDefault(AppConstants.Docker.ComposeProjectLabel);
    public string? ServiceName => _container.Labels?.GetValueOrDefault(AppConstants.Docker.ComposeServiceLabel);
    public bool IsPartOfStack => !string.IsNullOrEmpty(StackName);
    public string StateLabel => State.ToString();

    public bool IsRunning => State == ContainerState.Running;
    public bool IsPaused => State == ContainerState.Paused;
    public bool IsStopped => State is ContainerState.Exited or ContainerState.Created;
    public bool IsRestarting => State == ContainerState.Restarting;
    public bool IsProblematic => State is ContainerState.Restarting or ContainerState.Dead;
    public bool CanRemove => IsStopped || IsProblematic;

    public void UpdateFrom(ContainerInfo container)
    {
        if (container.Id != Id)
            return;

        _container = container;

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(State));
        OnPropertyChanged(nameof(StateLabel));
        OnPropertyChanged(nameof(StackName));
        OnPropertyChanged(nameof(ServiceName));
        OnPropertyChanged(nameof(IsPartOfStack));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(IsStopped));
        OnPropertyChanged(nameof(IsRestarting));
        OnPropertyChanged(nameof(IsProblematic));
        OnPropertyChanged(nameof(CanRemove));
        OnPropertyChanged(nameof(CreatedRelative));
    }

    public void StartStatsMonitoring()
    {
        StopStatsMonitoring();
        _statsTokenSource = new CancellationTokenSource();
        _ = MonitorStats(_statsTokenSource.Token);
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
        }
    }

    private async Task MonitorStats(CancellationToken cancellationToken)
    {
        if (_dockerClient == null)
        {
            CpuUsage = MemoryUsage = NetworkIO = DiskIO = "N/A";
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var parameters = new ContainerStatsParameters { Stream = false };
                ContainerStatsResponse? latestStats = null;
                var progress = new SynchronousProgress<ContainerStatsResponse>(stats => latestStats = stats);

                await _dockerClient.Containers.GetContainerStatsAsync(Id, parameters, progress, cancellationToken);

                if (latestStats != null && !cancellationToken.IsCancellationRequested)
                    UpdateStatsFromResponse(latestStats);

                await Task.Delay(AppConstants.Timing.StatsUpdateInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                CpuUsage = MemoryUsage = NetworkIO = DiskIO = "--";
                try
                {
                    await Task.Delay(AppConstants.Timing.StatsErrorRetryDelay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }
    }

    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public SynchronousProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
    }

    private void UpdateStatsFromResponse(ContainerStatsResponse stats)
    {
        try
        {
            var cpuDelta = stats.CPUStats.CPUUsage.TotalUsage - stats.PreCPUStats.CPUUsage.TotalUsage;
            var systemDelta = stats.CPUStats.SystemUsage - stats.PreCPUStats.SystemUsage;
            var onlineCpus = stats.CPUStats.OnlineCPUs > 0 ? stats.CPUStats.OnlineCPUs : 1;
            var cpuPercent = systemDelta > 0 ? (cpuDelta / (double)systemDelta) * onlineCpus * 100.0 : 0;
            CpuUsage = $"{cpuPercent:F1}%";

            var memoryMB = stats.MemoryStats.Usage / (1024.0 * 1024.0);
            var limitMB = stats.MemoryStats.Limit / (1024.0 * 1024.0);
            var memoryPercent = limitMB > 0 ? (memoryMB / limitMB) * 100 : 0;
            MemoryUsage = $"{memoryMB:F0} MB ({memoryPercent:F0}%)";

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

            if (stats.BlkioStats?.IoServiceBytesRecursive is { Count: > 0 } io)
            {
                long totalRead = 0, totalWrite = 0;
                foreach (var entry in io)
                {
                    if (string.Equals(entry.Op, "read", StringComparison.OrdinalIgnoreCase)) totalRead += (long)entry.Value;
                    if (string.Equals(entry.Op, "write", StringComparison.OrdinalIgnoreCase)) totalWrite += (long)entry.Value;
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
        }
    }

    private static string FormatBytes(long bytes)
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

    public void Dispose() => StopStatsMonitoring();
}
