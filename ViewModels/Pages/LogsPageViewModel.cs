using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class LogsPageViewModel : PageViewModel
{
    private static readonly string[] ErrorKeywords = { "error", "exception", "fatal", "critical", "failed" };

    private readonly IContainerLogs _logs;
    private readonly AppState _state;

    public LogsPageViewModel(IContainerLogs logs, IUiDispatcher ui, AppState state)
    {
        _logs = logs;
        _state = state;
        Logs = new LogBufferViewModel(logs, ui);
    }

    public override string Title => "Logs";
    public override string IconKey => "Icon.ScrollText";

    /// <summary>The live log view for the selected container.</summary>
    public LogBufferViewModel Logs { get; }

    /// <summary>Containers offered in the picker; the shell fills this from the containers page.</summary>
    public ObservableCollection<ContainerViewModel> AvailableContainers { get; } = new();

    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    [ObservableProperty]
    private bool _showingErrorOverview;

    public ObservableCollection<ErrorContainerInfo> ErrorContainers { get; } = new();

    public bool HasErrors => ErrorContainers.Count > 0;

    partial void OnSelectedContainerChanged(ContainerViewModel? value)
    {
        if (value is null) return;
        ShowingErrorOverview = false;
        Logs.Follow(value.Id, value.Name);
    }

    /// <summary>Searches the recent logs of every running container for the current filter.</summary>
    [RelayCommand]
    private async Task SearchAllLogsAsync()
    {
        var filter = Logs.SearchFilter;
        if (string.IsNullOrWhiteSpace(filter)) return;

        ShowingErrorOverview = false;
        SelectedContainer = null;
        var report = new StringBuilder();
        report.AppendLine($"=== Searching all running containers for '{filter}' ===").AppendLine();

        foreach (var container in AvailableContainers.Where(c => c.IsRunning).ToList())
        {
            var lines = await TailLinesAsync(container, timestamps: Logs.ShowTimestamps);
            var matches = lines.Where(l => l.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) continue;

            report.AppendLine($"--- {container.Name} ({matches.Count} matches) ---");
            foreach (var line in matches)
                report.AppendLine(line);
            report.AppendLine();
        }

        report.AppendLine("=== Search complete ===");
        Logs.SearchFilter = string.Empty;
        Logs.ShowText(report.ToString());
    }

    /// <summary>Lists running containers whose recent logs mention an error keyword.</summary>
    [RelayCommand]
    private async Task ShowErrorsAsync()
    {
        ShowingErrorOverview = true;
        ErrorContainers.Clear();

        foreach (var container in AvailableContainers.Where(c => c.IsRunning).ToList())
        {
            var errorLines = (await TailLinesAsync(container, timestamps: false))
                .Where(l => ErrorKeywords.Any(k => l.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Take(10)
                .ToList();

            if (errorLines.Count > 0)
                ErrorContainers.Add(new ErrorContainerInfo(container.Id, container.Name, errorLines));
        }

        OnPropertyChanged(nameof(HasErrors));
    }

    [RelayCommand]
    private void ViewContainerLogs(ErrorContainerInfo info)
    {
        var container = AvailableContainers.FirstOrDefault(c => c.Id == info.ContainerId);
        if (container is null) return;
        SelectedContainer = container;
    }

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        var path = await Logs.ExportAsync();
        _state.StatusMessage = path is null ? "Failed to export logs" : $"Logs exported to {path}";
    }

    private async Task<string[]> TailLinesAsync(ContainerViewModel container, bool timestamps)
    {
        try
        {
            var text = await _logs.ReadTailAsync(container.Id, timestamps, 100, CancellationToken.None);
            return text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
        }
        catch (Exception)
        {
            return Array.Empty<string>();
        }
    }

    public override void Dispose() => Logs.Dispose();
}

public sealed record ErrorContainerInfo(string ContainerId, string ContainerName, List<string> ErrorLines)
{
    public int ErrorCount => ErrorLines.Count;
    public string ErrorCountLabel => ErrorCount == 1 ? "1 error" : $"{ErrorCount} errors";
}
