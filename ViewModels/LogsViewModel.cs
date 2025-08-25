using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using Docker.DotNet.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

public partial class LogsViewModel : ObservableObject, IDisposable
{
    private readonly string _containerId;
    private readonly DockerClient _dockerClient;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly StringBuilder _logsBuilder = new();

    [ObservableProperty]
    private string _containerName;

    [ObservableProperty]
    private string _logsContent = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showTimestamps = true;

    [ObservableProperty]
    private string _statusMessage = "Connecting to container...";

    public string ContainerId => _containerId.Length > 12 ? _containerId.Substring(0, 12) : _containerId;
    
    public LogsViewModel(string containerId, string containerName, DockerClient dockerClient)
    {
        _containerId = containerId;
        _containerName = containerName;
        _dockerClient = dockerClient;
        _ = StartStreamingLogs();
    }

    private async Task StartStreamingLogs()
    {
        try
        {
            StatusMessage = "Fetching logs...";
            
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

            StatusMessage = "Streaming logs...";

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
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _logsBuilder.AppendLine($"[ERROR] Failed to stream logs: {ex.Message}");
            LogsContent = _logsBuilder.ToString();
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logsBuilder.Clear();
        LogsContent = string.Empty;
        StatusMessage = "Logs cleared";
    }

    [RelayCommand]
    private async Task ExportLogs()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = $"{ContainerName}_logs_{timestamp}.txt";
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, fileName);
            
            await File.WriteAllTextAsync(filePath, LogsContent, _cancellationTokenSource.Token);
            StatusMessage = $"Logs exported to {fileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
    }

    partial void OnShowTimestampsChanged(bool value)
    {
        _cancellationTokenSource.Cancel();
        _logsBuilder.Clear();
        LogsContent = string.Empty;
        
        _ = StartStreamingLogs();
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
    }
}