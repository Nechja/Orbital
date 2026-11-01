using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

/// <summary>
/// A live log view for one container: follows its output, keeps the last N lines, and exposes
/// the filtered text for display. Shared by the Logs page and the container detail pane.
/// </summary>
public sealed partial class LogBufferViewModel : ObservableObject, IDisposable
{
    private readonly IContainerLogs _logs;
    private readonly IUiDispatcher _ui;
    private readonly List<string> _lines = new();
    private string _partialLine = string.Empty;
    private CancellationTokenSource? _cts;

    public LogBufferViewModel(IContainerLogs logs, IUiDispatcher ui)
    {
        _logs = logs;
        _ui = ui;
    }

    /// <summary>The text to display: all buffered lines, or only those matching <see cref="SearchFilter"/>.</summary>
    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private bool _showTimestamps = true;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    [ObservableProperty]
    private string? _containerId;

    [ObservableProperty]
    private string? _containerName;

    public bool IsFollowing => _cts is not null;

    /// <summary>Starts following a container's logs, replacing whatever was shown.</summary>
    public void Follow(string containerId, string containerName)
    {
        Stop();
        ContainerId = containerId;
        ContainerName = containerName;
        Reset();

        _cts = new CancellationTokenSource();
        _ = RunAsync(containerId, ShowTimestamps, _cts.Token);
        OnPropertyChanged(nameof(IsFollowing));
    }

    /// <summary>Stops following but keeps the text on screen.</summary>
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        OnPropertyChanged(nameof(IsFollowing));
    }

    /// <summary>Shows static text (e.g. a cross-container search result) instead of a live stream.</summary>
    public void ShowText(string text)
    {
        Stop();
        ContainerId = null;
        ContainerName = null;
        Reset();
        Append(text);
    }

    [RelayCommand]
    private void Clear()
    {
        Reset();
    }

    /// <summary>Writes the displayed text to the desktop. Returns the path, or null on failure.</summary>
    public async Task<string?> ExportAsync()
    {
        try
        {
            var name = $"{ContainerName ?? "logs"}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), name);
            await File.WriteAllTextAsync(path, Content);
            return path;
        }
        catch (Exception)
        {
            return null;
        }
    }

    partial void OnSearchFilterChanged(string value) => Render();

    partial void OnShowTimestampsChanged(bool value)
    {
        if (ContainerId is { } id && ContainerName is { } name && IsFollowing)
            Follow(id, name);
    }

    private async Task RunAsync(string containerId, bool timestamps, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var chunk in _logs.FollowAsync(containerId, timestamps, 100, cancellationToken))
                await _ui.InvokeAsync(() => Append(chunk));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await _ui.InvokeAsync(() => Append($"\n[ERROR] Failed to stream logs: {ex.Message}\n"));
        }
    }

    private void Reset()
    {
        _lines.Clear();
        _partialLine = string.Empty;
        Content = string.Empty;
    }

    private void Append(string chunk)
    {
        var text = _partialLine + chunk;
        var parts = text.Split('\n');
        _lines.AddRange(parts.Take(parts.Length - 1));
        _partialLine = parts[^1];

        var overflow = _lines.Count - AppConstants.UI.MaxLogLines;
        if (overflow > 0)
            _lines.RemoveRange(0, overflow);

        Render();
    }

    private void Render()
    {
        IEnumerable<string> lines = _lines;
        if (_partialLine.Length > 0)
            lines = lines.Append(_partialLine);
        if (!string.IsNullOrWhiteSpace(SearchFilter))
            lines = lines.Where(l => l.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        Content = string.Join('\n', lines);
    }

    public void Dispose() => Stop();
}
