using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace OrbitalDocking.ViewModels;

public partial class LogsViewModel : ObservableObject, IDisposable
{
    private readonly DockerClient _dockerClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly StringBuilder _logsBuilder = new();

    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _availableContainers = new();

    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    [ObservableProperty]
    private string _logsContent = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showTimestamps = true;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private bool _showingErrorOverview = false;

    [ObservableProperty]
    private ObservableCollection<ErrorContainerInfo> _errorContainers = new();

    public bool HasErrors => ErrorContainers.Any();

    public LogsViewModel(DockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    partial void OnSelectedContainerChanged(ContainerViewModel? value)
    {
        if (value != null)
        {
            _ = LoadContainerLogsAsync(value.Id, value.Name);
        }
    }

    partial void OnSearchFilterChanged(string value)
    {
        FilterLogs();
    }

    public async Task LoadContainerLogsAsync(string containerId, string containerName)
    {
        // Stop previous stream if any
        StopStreaming();

        _logsBuilder.Clear();
        LogsContent = string.Empty;
        ShowingErrorOverview = false;

        // Start new stream
        _cancellationTokenSource = new CancellationTokenSource();
        await StartStreamingLogsAsync(containerId, containerName);
    }

    private async Task StartStreamingLogsAsync(string containerId, string containerName)
    {
        if (_cancellationTokenSource == null) return;

        try
        {
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = true,
                Timestamps = ShowTimestamps,
                Tail = "100"
            };

            var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                containerId,
                false,
                parameters,
                _cancellationTokenSource.Token);

            var buffer = new byte[4096];
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, _cancellationTokenSource.Token);
                if (result.Count > 0)
                {
                    var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    _logsBuilder.Append(text);
                    FilterLogs();
                }
                else if (result.EOF)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping stream
        }
        catch (Exception ex)
        {
            _logsBuilder.AppendLine($"\n[ERROR] Failed to stream logs: {ex.Message}");
            LogsContent = _logsBuilder.ToString();
        }
    }

    private void FilterLogs()
    {
        var allLogs = _logsBuilder.ToString();

        if (string.IsNullOrWhiteSpace(SearchFilter))
        {
            LogsContent = allLogs;
        }
        else
        {
            var lines = allLogs.Split('\n');
            var filtered = lines.Where(line =>
                line.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
            LogsContent = string.Join('\n', filtered);
        }
    }

    [RelayCommand]
    private async Task SearchAllLogs()
    {
        if (string.IsNullOrWhiteSpace(SearchFilter))
        {
            return;
        }

        StopStreaming();
        ShowingErrorOverview = false;
        _logsBuilder.Clear();
        _logsBuilder.AppendLine($"=== SEARCHING ALL CONTAINERS FOR: '{SearchFilter}' ===\n");

        var containers = AvailableContainers.Where(c => c.IsRunning).ToList();

        foreach (var container in containers)
        {
            try
            {
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = false,
                    Timestamps = ShowTimestamps,
                    Tail = "100"
                };

                var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                    container.Id,
                    false,
                    parameters,
                    CancellationToken.None);

                var containerLogs = new StringBuilder();
                var buffer = new byte[4096];

                while (true)
                {
                    var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    if (result.Count > 0)
                    {
                        containerLogs.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.EOF)
                    {
                        break;
                    }
                }

                var logs = containerLogs.ToString();
                var matchingLines = logs.Split('\n')
                    .Where(line => line.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (matchingLines.Any())
                {
                    _logsBuilder.AppendLine($"──────────────────────────────────────");
                    _logsBuilder.AppendLine($"📦 {container.Name} ({matchingLines.Count} matches)");
                    _logsBuilder.AppendLine($"──────────────────────────────────────");
                    foreach (var line in matchingLines)
                    {
                        _logsBuilder.AppendLine(line);
                    }
                    _logsBuilder.AppendLine();
                }
            }
            catch (Exception ex)
            {
                _logsBuilder.AppendLine($"[ERROR] Failed to search logs for {container.Name}: {ex.Message}");
            }
        }

        _logsBuilder.AppendLine("\n=== SEARCH COMPLETE ===");
        LogsContent = _logsBuilder.ToString();
    }

    [RelayCommand]
    private async Task ShowErrors()
    {
        ShowingErrorOverview = true;
        ErrorContainers.Clear();

        var containers = AvailableContainers.Where(c => c.IsRunning).ToList();
        var errorKeywords = new[] { "error", "exception", "fatal", "critical", "failed" };

        foreach (var container in containers)
        {
            try
            {
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                    Follow = false,
                    Timestamps = false,
                    Tail = "100"
                };

                var stream = await _dockerClient.Containers.GetContainerLogsAsync(
                    container.Id,
                    false,
                    parameters,
                    CancellationToken.None);

                var containerLogs = new StringBuilder();
                var buffer = new byte[4096];

                while (true)
                {
                    var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, CancellationToken.None);
                    if (result.Count > 0)
                    {
                        containerLogs.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    else if (result.EOF)
                    {
                        break;
                    }
                }

                var logs = containerLogs.ToString();
                var errorCount = logs.Split('\n')
                    .Count(line => errorKeywords.Any(keyword =>
                        line.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

                if (errorCount > 0)
                {
                    ErrorContainers.Add(new ErrorContainerInfo
                    {
                        ContainerId = container.Id,
                        ContainerName = container.Name,
                        ErrorCount = errorCount
                    });
                }
            }
            catch (Exception)
            {
                // Silently skip containers we can't access
            }
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private async Task ViewContainerLogs(ErrorContainerInfo? errorInfo)
    {
        if (errorInfo == null) return;

        var container = AvailableContainers.FirstOrDefault(c => c.Id == errorInfo.ContainerId);
        if (container != null)
        {
            ShowingErrorOverview = false;
            SelectedContainer = container;
            await LoadContainerLogsAsync(container.Id, container.Name);
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logsBuilder.Clear();
        LogsContent = string.Empty;
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var containerName = SelectedContainer?.Name ?? "all";
            var fileName = $"{containerName}_logs_{timestamp}.txt";
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, fileName);

            await File.WriteAllTextAsync(filePath, LogsContent);
        }
        catch (Exception)
        {
            // Silently fail for now - could add status message later
        }
    }

    private void StopStreaming()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    partial void OnShowTimestampsChanged(bool value)
    {
        if (SelectedContainer == null) return;

        // Restart streaming with new timestamp setting
        StopStreaming();
        _logsBuilder.Clear();
        LogsContent = string.Empty;

        _cancellationTokenSource = new CancellationTokenSource();
        _ = StartStreamingLogsAsync(SelectedContainer.Id, SelectedContainer.Name);
    }

    public void Dispose()
    {
        StopStreaming();
    }
}

public class ErrorContainerInfo
{
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
}
