using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace OrbitalDocking.ViewModels;

public partial class LogsPanelViewModel : ObservableObject, IDisposable
{
    private readonly DockerClient _dockerClient;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly StringBuilder _logsBuilder = new();
    private string _containerId = string.Empty;

    [ObservableProperty]
    private string _containerName = string.Empty;

    [ObservableProperty]
    private string _logsContent = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showTimestamps = true;

    [ObservableProperty]
    private bool _isVisible = false;

    public string ContainerId => _containerId.Length > 12 ? _containerId[..12] : _containerId;

    public event EventHandler? CloseRequested;

    public LogsPanelViewModel(DockerClient dockerClient)
    {
        _dockerClient = dockerClient;
    }

    public async Task LoadContainerLogsAsync(string containerId, string containerName)
    {
        // Stop previous stream if any
        StopStreaming();

        _containerId = containerId;
        ContainerName = containerName;
        _logsBuilder.Clear();
        LogsContent = string.Empty;
        IsVisible = true;

        // Start new stream
        _cancellationTokenSource = new CancellationTokenSource();
        await StartStreamingLogsAsync();
    }

    private async Task StartStreamingLogsAsync()
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
                _containerId,
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
                    LogsContent = _logsBuilder.ToString();
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
            var fileName = $"{ContainerName}_logs_{timestamp}.txt";
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, fileName);

            await File.WriteAllTextAsync(filePath, LogsContent);
        }
        catch (Exception)
        {
            // Silently fail for now - could add status message later
        }
    }

    [RelayCommand]
    private void Close()
    {
        StopStreaming();
        IsVisible = false;
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void StopStreaming()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    partial void OnShowTimestampsChanged(bool value)
    {
        if (!IsVisible) return;

        // Restart streaming with new timestamp setting
        StopStreaming();
        _logsBuilder.Clear();
        LogsContent = string.Empty;

        _cancellationTokenSource = new CancellationTokenSource();
        _ = StartStreamingLogsAsync();
    }

    public void Dispose()
    {
        StopStreaming();
    }
}
