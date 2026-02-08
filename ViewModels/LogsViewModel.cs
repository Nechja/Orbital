using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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
    private int _lastProcessedLength = 0;
    private string? _incompleteLine = null;
    private readonly List<string> _logLines = new();

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
        if (value is not null)
            _ = LoadContainerLogsAsync(value.Id, value.Name);
    }

    partial void OnSearchFilterChanged(string value)
    {
        _lastProcessedLength = 0;
        _logLines.Clear();
        _incompleteLine = null;
        FilterLogs();
    }

    public async Task LoadContainerLogsAsync(string containerId, string containerName)
    {
        StopStreaming();

        _logsBuilder.Clear();
        _logLines.Clear();
        _lastProcessedLength = 0;
        _incompleteLine = null;

        await Dispatcher.UIThread.InvokeAsync(() => LogsContent = string.Empty);
        ShowingErrorOverview = false;

        _cancellationTokenSource = new CancellationTokenSource();
        await StartStreamingLogsAsync(containerId, containerName);
    }

    private async Task StartStreamingLogsAsync(string containerId, string containerName)
    {
        if (_cancellationTokenSource is null) return;

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
                    await Dispatcher.UIThread.InvokeAsync(() => FilterLogs());
                }
                else if (result.EOF)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
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
            return;
        }

        if (allLogs.Length < _lastProcessedLength)
        {
            _logLines.Clear();
            _incompleteLine = null;
            _lastProcessedLength = 0;
        }

        if (allLogs.Length > _lastProcessedLength)
        {
            var newText = allLogs.AsSpan(_lastProcessedLength);
            var start = 0;

            for (var i = 0; i < newText.Length; i++)
            {
                if (newText[i] == '\n')
                {
                    var lineSpan = newText.Slice(start, i - start);
                    var lineText = lineSpan.ToString();

                    if (!string.IsNullOrEmpty(_incompleteLine))
                    {
                        lineText = _incompleteLine + lineText;
                        _incompleteLine = null;
                    }

                    _logLines.Add(lineText);
                    start = i + 1;
                }
            }

            if (start < newText.Length)
            {
                var tail = newText.Slice(start).ToString();
                _incompleteLine = string.IsNullOrEmpty(_incompleteLine) ? tail : _incompleteLine + tail;
            }

            _lastProcessedLength = allLogs.Length;
        }

        var sourceLines = (IEnumerable<string>)_logLines;
        if (!string.IsNullOrEmpty(_incompleteLine))
            sourceLines = sourceLines.Append(_incompleteLine);

        var filtered = sourceLines.Where(line =>
            line.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

        LogsContent = string.Join('\n', filtered);
    }

    [RelayCommand]
    private async Task SearchAllLogs()
    {
        if (string.IsNullOrWhiteSpace(SearchFilter))
            return;

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
                var errorLines = logs.Split('\n')
                    .Where(line => errorKeywords.Any(keyword =>
                        line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
                    .Take(10) // Limit to 10 error lines per container
                    .ToList();

                if (errorLines.Any())
                {
                    ErrorContainers.Add(new ErrorContainerInfo
                    {
                        ContainerId = container.Id,
                        ContainerName = container.Name,
                        ErrorCount = errorLines.Count,
                        ErrorLines = errorLines
                    });
                }
            }
            catch (Exception)
            {
            }
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private async Task ViewContainerLogs(ErrorContainerInfo? errorInfo)
    {
        if (errorInfo is null) return;

        var container = AvailableContainers.FirstOrDefault(c => c.Id == errorInfo.ContainerId);
        if (container is not null)
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
        if (SelectedContainer is null) return;

        StopStreaming();
        _logsBuilder.Clear();
        LogsContent = string.Empty;

        _cancellationTokenSource = new CancellationTokenSource();
        _ = StartStreamingLogsAsync(SelectedContainer.Id, SelectedContainer.Name);
    }

    public void Dispose() => StopStreaming();
}

public class ErrorContainerInfo
{
    public string ContainerId { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
    public List<string> ErrorLines { get; set; } = new();
}
