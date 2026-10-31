using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using Microsoft.Extensions.Logging;
using OrbitalDocking.Configuration;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class ContainersPageViewModel : DockerPageViewModel
{
    private readonly DockerClient? _dockerClient;
    private readonly IDialogService _dialogs;
    private readonly ILogger<ContainersPageViewModel> _logger;
    private readonly Dictionary<string, ContainerViewModel> _byId = new();

    public ContainersPageViewModel(
        IDockerService docker,
        DockerClient? dockerClient,
        IDialogService dialogs,
        AppState state,
        IUiDispatcher ui,
        ILogger<ContainersPageViewModel> logger)
        : base(docker, state, ui)
    {
        _dockerClient = dockerClient;
        _dialogs = dialogs;
        _logger = logger;

        var dockerEvents = Observable.FromEventPattern<ContainerEventArgs>(
            h => Docker.ContainerEvent += h,
            h => Docker.ContainerEvent -= h);

        Subscriptions.Add(dockerEvents
            .Do(e => _logger.LogDebug("Docker event: {Action} for {ContainerId}", e.EventArgs.Action, e.EventArgs.ContainerId))
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(_ => RefreshAsync()));
    }

    private bool _started;

    public override void Activate()
    {
        if (_started)
        {
            _ = RefreshAsync();
            return;
        }

        _started = true;
        StartPeriodicRefresh(TimeSpan.Zero, AppConstants.Timing.ContainerRefreshInterval);
    }

    public override string Title => "Containers";
    public override string IconKey => "Icon.Box";

    /// <summary>Every known container, unfiltered. View model instances are reused across refreshes.</summary>
    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    /// <summary>Compose stacks matching the search, each holding its containers.</summary>
    public ObservableCollection<StackViewModel> Stacks { get; } = new();

    /// <summary>Containers matching the search that are not part of a stack.</summary>
    public ObservableCollection<ContainerViewModel> StandaloneContainers { get; } = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _runningCount;

    [ObservableProperty]
    private int _stoppedCount;

    /// <summary>The container shown in the detail pane, if any.</summary>
    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    /// <summary>Raised when the user asks to see a container's logs; the shell navigates to the logs page.</summary>
    public event EventHandler<ContainerViewModel>? LogsRequested;

    partial void OnSearchTextChanged(string value) => GroupByStack();

    partial void OnSelectedContainerChanged(ContainerViewModel? oldValue, ContainerViewModel? newValue)
    {
        if (oldValue is not null)
        {
            oldValue.IsSelected = false;
            oldValue.StopStatsMonitoring();
        }

        if (newValue is not null)
        {
            newValue.IsSelected = true;
            if (newValue.IsRunning)
                newValue.StartStatsMonitoring();
        }
    }

    /// <summary>Selects a container for the detail pane; selecting it again deselects.</summary>
    [RelayCommand]
    private void Select(ContainerViewModel? container) =>
        SelectedContainer = ReferenceEquals(container, SelectedContainer) ? null : container;

    [RelayCommand]
    private void CloseDetails() => SelectedContainer = null;

    [RelayCommand]
    public override Task RefreshAsync() => GuardedRefreshAsync(async () =>
    {
        var result = await Docker.GetContainersAsync();
        if (IsDisposed) return;

        await Ui.InvokeAsync(() =>
        {
            if (result.IsError)
                State.StatusMessage = $"Error: {result.FirstError.Description}";
            else
                Apply(result.Value);
        });
    });

    [RelayCommand]
    private Task StartContainerAsync(ContainerViewModel container) =>
        RunAsync($"Starting {container.Name}...", $"Started {container.Name}", () => Docker.StartContainerAsync(container.Id));

    [RelayCommand]
    private Task StopContainerAsync(ContainerViewModel container) =>
        RunAsync($"Stopping {container.Name}...", $"Stopped {container.Name}", () => Docker.StopContainerAsync(container.Id));

    [RelayCommand]
    private Task RestartContainerAsync(ContainerViewModel container) =>
        RunAsync($"Restarting {container.Name}...", $"Restarted {container.Name}", () => Docker.RestartContainerAsync(container.Id));

    [RelayCommand]
    private async Task RemoveContainerAsync(ContainerViewModel container)
    {
        var infoResult = await Docker.GetContainerAsync(container.Id);
        if (infoResult.IsError)
        {
            State.StatusMessage = $"Failed to get container info: {infoResult.FirstError.Description}";
            return;
        }

        var volumes = infoResult.Value.Volumes ?? new List<VolumeMount>();
        var choice = await AskAboutVolumesAsync(container.Name, volumes);
        if (choice == VolumeRemovalChoice.Cancel)
        {
            State.StatusMessage = "Container removal cancelled";
            return;
        }

        State.StatusMessage = $"Removing {container.Name}...";
        var result = await Docker.RemoveContainerAsync(container.Id, force: true);
        if (result.IsError)
        {
            State.StatusMessage = result.FirstError.Description;
            return;
        }

        var removedVolumes = choice == VolumeRemovalChoice.RemoveContainerAndVolumes && await RemoveNamedVolumesAsync(volumes);
        State.StatusMessage = removedVolumes ? $"Removed {container.Name} and its volumes" : $"Removed {container.Name}";
        await RefreshAsync();
    }

    [RelayCommand]
    private void ShowLogs(ContainerViewModel container) => LogsRequested?.Invoke(this, container);

    [RelayCommand]
    private Task StartStackAsync(StackViewModel stack) =>
        RunStackOperationAsync(stack, "Starting", "Started", c => !c.IsRunning, c => Docker.StartContainerAsync(c.Id));

    [RelayCommand]
    private Task StopStackAsync(StackViewModel stack) =>
        RunStackOperationAsync(stack, "Stopping", "Stopped", c => c.IsRunning, c => Docker.StopContainerAsync(c.Id));

    [RelayCommand]
    private Task RestartStackAsync(StackViewModel stack) =>
        RunStackOperationAsync(stack, "Restarting", "Restarted", _ => true, c => Docker.RestartContainerAsync(c.Id));

    [RelayCommand]
    private async Task RemoveStackAsync(StackViewModel stack)
    {
        var volumes = new List<VolumeMount>();
        foreach (var container in stack.Containers)
        {
            var info = await Docker.GetContainerAsync(container.Id);
            if (!info.IsError && info.Value.Volumes is { } mounts)
                volumes.AddRange(mounts);
        }

        var choice = await AskAboutVolumesAsync($"stack '{stack.Name}'", volumes);
        if (choice == VolumeRemovalChoice.Cancel)
        {
            State.StatusMessage = "Stack removal cancelled";
            return;
        }

        State.StatusMessage = $"Removing stack {stack.Name}...";
        var result = await Docker.RemoveStackAsync(stack.Name, stack.Containers.Select(c => c.Id).ToList());
        if (result.IsError)
        {
            State.StatusMessage = $"Failed to remove stack {stack.Name}: {result.FirstError.Description}";
            return;
        }

        var removedVolumes = choice == VolumeRemovalChoice.RemoveContainerAndVolumes && await RemoveNamedVolumesAsync(volumes);
        State.StatusMessage = removedVolumes ? $"Removed stack {stack.Name} and its volumes" : $"Removed stack {stack.Name}";
        await RefreshAsync();
    }

    [RelayCommand]
    private Task PruneContainersAsync() =>
        RunAsync("Pruning containers...", "Containers pruned", () => Docker.PruneContainersAsync());

    private async Task RunStackOperationAsync(
        StackViewModel stack,
        string inProgressVerb,
        string completedVerb,
        Func<ContainerViewModel, bool> predicate,
        Func<ContainerViewModel, Task> operation)
    {
        State.StatusMessage = $"{inProgressVerb} stack {stack.Name}...";
        foreach (var container in stack.Containers.Where(predicate).ToList())
            await operation(container);
        State.StatusMessage = $"{completedVerb} stack {stack.Name}";
        await RefreshAsync();
    }

    private Task<VolumeRemovalChoice> AskAboutVolumesAsync(string subject, List<VolumeMount> volumes)
    {
        var named = volumes.Where(v => !string.IsNullOrEmpty(v.Name)).GroupBy(v => v.Name).Select(g => g.First()).ToList();
        return named.Count == 0
            ? Task.FromResult(VolumeRemovalChoice.RemoveContainerOnly)
            : _dialogs.ShowVolumeRemovalDialogAsync(subject, named);
    }

    /// <summary>Removes the named volumes among <paramref name="volumes"/>. Returns true if there were any.</summary>
    private async Task<bool> RemoveNamedVolumesAsync(IEnumerable<VolumeMount> volumes)
    {
        var names = volumes.Select(v => v.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        foreach (var name in names)
        {
            State.StatusMessage = $"Removing volume {name}...";
            var result = await Docker.RemoveVolumeAsync(name, force: true);
            if (result.IsError)
                _logger.LogWarning("Failed to remove volume {Volume}: {Error}", name, result.FirstError.Description);
        }
        return names.Count > 0;
    }

    private void Apply(IEnumerable<ContainerInfo> containers)
    {
        var incoming = containers.ToList();
        var incomingIds = incoming.Select(c => c.Id).ToHashSet();

        foreach (var stale in Containers.Where(c => !incomingIds.Contains(c.Id)).ToList())
        {
            if (ReferenceEquals(stale, SelectedContainer))
                SelectedContainer = null;
            Containers.Remove(stale);
            _byId.Remove(stale.Id);
            stale.Dispose();
        }

        foreach (var info in incoming)
        {
            if (_byId.TryGetValue(info.Id, out var existing))
            {
                existing.UpdateFrom(info);
            }
            else
            {
                var vm = new ContainerViewModel(info, _dockerClient);
                _byId[info.Id] = vm;
                Containers.Add(vm);
            }
        }

        Count = Containers.Count;
        RunningCount = Containers.Count(c => c.IsRunning);
        StoppedCount = Containers.Count(c => c.IsStopped);
        GroupByStack();
    }

    private bool MatchesSearch(ContainerViewModel c)
    {
        var search = SearchText.Trim();
        if (search.Length == 0) return true;
        return Contains(c.Name) || Contains(c.Image) || Contains(c.Id) || Contains(c.StackName);

        bool Contains(string? text) => text?.Contains(search, StringComparison.OrdinalIgnoreCase) == true;
    }

    private void GroupByStack()
    {
        var visible = Containers.Where(MatchesSearch).ToList();
        var existingStacks = Stacks.ToDictionary(s => s.Name);

        Stacks.Clear();
        foreach (var group in visible.Where(c => c.IsPartOfStack).GroupBy(c => c.StackName!).OrderBy(g => g.Key))
        {
            var stack = existingStacks.TryGetValue(group.Key, out var known) ? known : new StackViewModel(group.Key);
            stack.Containers.Clear();
            foreach (var container in group.OrderBy(c => c.ServiceName).ThenBy(c => c.Name))
                stack.Containers.Add(container);
            stack.NotifyStateChanged();
            Stacks.Add(stack);
        }

        StandaloneContainers.Clear();
        foreach (var container in visible.Where(c => !c.IsPartOfStack).OrderBy(c => c.Name))
            StandaloneContainers.Add(container);
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var container in Containers)
            container.Dispose();
    }
}
