using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using OrbitalDocking.Configuration;
using OrbitalDocking.Extensions;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IThemeService _themeService;
    private readonly DockerClient _dockerClient;
    private readonly IDialogService _dialogService;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly SemaphoreSlim _containerSemaphore = new(1, 1);
    private readonly SemaphoreSlim _imageSemaphore = new(1, 1);
    private readonly SemaphoreSlim _volumeSemaphore = new(1, 1);
    private readonly SemaphoreSlim _networkSemaphore = new(1, 1);
    private readonly SourceCache<ContainerViewModel, string> _containerCache = new(x => x.Id);

    public Window? MainWindow { get; set; }
    
    public MainWindowViewModel(
        IDockerService dockerService, 
        IThemeService themeService, 
        DockerClient dockerClient, 
        IDialogService dialogService,
        ILogger<MainWindowViewModel> logger)
    {
        _dockerService = dockerService;
        _themeService = themeService;
        _dockerClient = dockerClient;
        _dialogService = dialogService;
        _logger = logger;
        
        var containers = new ObservableCollectionExtended<ContainerViewModel>();
        _containers = containers;
        _subscriptions.Add(_containerCache.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(containers)
            .Subscribe());
        
        var dockerEvents = Observable.FromEventPattern<ContainerEventArgs>(
            h => _dockerService.ContainerEvent += h,
            h => _dockerService.ContainerEvent -= h);
        
        _subscriptions.Add(dockerEvents
            .Do(e => _logger.LogDebug("Docker event: {Action} for {ContainerId}", e.EventArgs.Action, e.EventArgs.ContainerId))
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Subscribe(async _ => await RefreshContainersAsync()));
        
        _subscriptions.Add(Observable.Timer(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(30))
            .Subscribe(async _ => await RefreshContainersAsync()));
        
        _subscriptions.Add(Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1))
            .Subscribe(async _ => await RefreshImagesAsync()));
            
        _subscriptions.Add(Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .Subscribe(async _ => await RefreshVolumesAsync()));
            
        _subscriptions.Add(Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(30))
            .Subscribe(async _ => await RefreshNetworksAsync()));
        
        _ = GetDockerVersionAsync();
        
        _ = Task.Run(async () => await _dockerService.StartMonitoringEventsAsync());
    }
    
    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _containers;
    
    [ObservableProperty]
    private ObservableCollection<StackViewModel> _stacks = new();
    
    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _standaloneContainers = new();

    [ObservableProperty]
    private ContainerViewModel? _selectedContainer;

    [ObservableProperty]
    private ObservableCollection<ImageViewModel> _images = new();

    [ObservableProperty]
    private ObservableCollection<VolumeViewModel> _volumes = new();

    [ObservableProperty]
    private ObservableCollection<NetworkViewModel> _networks = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private ResourceType _selectedResourceType = ResourceType.Container;

    [ObservableProperty]
    private bool _showContainers = true;

    [ObservableProperty]
    private bool _showImages = false;
    
    [ObservableProperty]
    private bool _showVolumes = false;
    
    [ObservableProperty]
    private bool _showNetworks = false;

    public string ContainersTextColor => ShowContainers ? "#FFFFFF" : "#8888AA";
    public string ImagesTextColor => ShowImages ? "#FFFFFF" : "#8888AA";
    public string VolumesTextColor => ShowVolumes ? "#FFFFFF" : "#8888AA";
    public string NetworksTextColor => ShowNetworks ? "#FFFFFF" : "#8888AA";

    [ObservableProperty]
    private string _dockerVersion = "Connecting...";

    [ObservableProperty]
    private string _dockerStatusColor = ThemeColors.Dark.TextTertiary;

    public int RunningContainersCount => Containers.Count(c => c.IsRunning);
    public int StoppedContainersCount => Containers.Count(c => c.IsStopped);


    private async Task GetDockerVersionAsync()
    {
        var result = await _dockerService.GetSystemInfoAsync();
        
        if (result.IsError)
        {
            DockerVersion = "Disconnected";
            DockerStatusColor = ThemeColors.Dark.DockerOffline;
            StatusMessage = result.FirstError.Description;
        }
        else
        {
            var systemInfo = result.Value;
            DockerVersion = $"v{systemInfo.ServerVersion}";
            DockerStatusColor = ThemeColors.Dark.DockerOnline;
            StatusMessage = "Connected to Docker";
        }
    }

    public ObservableCollection<ContainerViewModel> FilteredContainers
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Containers;

            var filtered = Containers.Where(c =>
                c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Image.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                c.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ObservableCollection<ContainerViewModel>(filtered);
        }
    }

    public ObservableCollection<ImageViewModel> FilteredImages
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Images;

            var filtered = Images.Where(i =>
                i.Repository.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.Tag.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                i.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ObservableCollection<ImageViewModel>(filtered);
        }
    }
    
    public ObservableCollection<VolumeViewModel> FilteredVolumes
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Volumes;

            var filtered = Volumes.Where(v =>
                v.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                v.Driver.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ObservableCollection<VolumeViewModel>(filtered);
        }
    }
    
    public ObservableCollection<NetworkViewModel> FilteredNetworks
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return Networks;

            var filtered = Networks.Where(n =>
                n.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Driver.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                n.Id.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return new ObservableCollection<NetworkViewModel>(filtered);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(FilteredImages));
        OnPropertyChanged(nameof(FilteredVolumes));
        OnPropertyChanged(nameof(FilteredNetworks));
    }

    [RelayCommand]
    private async Task RefreshContainersAsync()
    {
        // Try to acquire the semaphore, skip if already refreshing
        if (!await _containerSemaphore.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            var result = await _dockerService.GetContainersAsync();
            
            if (result.IsError)
            {
                StatusMessage = $"Error: {result.FirstError.Description}";
            }
            else
            {
                UpdateContainerList(result.Value);
            }
        }
        finally
        {
            IsLoading = false;
            _containerSemaphore.Release();
        }
    }

    [RelayCommand]
    private async Task StartContainerAsync(ContainerViewModel? container = null)
    {
        container ??= SelectedContainer;
        if (container == null) return;

        StatusMessage = $"Starting {container.Name}...";
        var result = await _dockerService.StartContainerAsync(container.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Started {container.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task StopContainerAsync(ContainerViewModel? container = null)
    {
        container ??= SelectedContainer;
        if (container == null) return;

        StatusMessage = $"Stopping {container.Name}...";
        var result = await _dockerService.StopContainerAsync(container.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Stopped {container.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RestartContainerAsync(ContainerViewModel? container = null)
    {
        container ??= SelectedContainer;
        if (container == null) return;

        StatusMessage = $"Restarting {container.Name}...";
        var result = await _dockerService.RestartContainerAsync(container.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Restarted {container.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RemoveContainerAsync(ContainerViewModel? container = null)
    {
        container ??= SelectedContainer;
        if (container == null) return;

        StatusMessage = $"Removing {container.Name}...";
        var result = await _dockerService.RemoveContainerAsync(container.Id, force: true);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Removed {container.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task StartStackAsync(StackViewModel? stack)
    {
        if (stack == null) return;
        
        StatusMessage = $"Starting stack {stack.Name}...";
        // Create a snapshot to avoid collection modification during iteration
        var containersToStart = stack.Containers.Where(c => !c.IsRunning).ToList();
        foreach (var container in containersToStart)
        {
            await _dockerService.StartContainerAsync(container.Id);
        }
        StatusMessage = $"Started stack {stack.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task StopStackAsync(StackViewModel? stack)
    {
        if (stack == null) return;
        
        StatusMessage = $"Stopping stack {stack.Name}...";
        // Create a snapshot to avoid collection modification during iteration
        var containersToStop = stack.Containers.Where(c => c.IsRunning).ToList();
        foreach (var container in containersToStop)
        {
            await _dockerService.StopContainerAsync(container.Id);
        }
        StatusMessage = $"Stopped stack {stack.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task RestartStackAsync(StackViewModel? stack)
    {
        if (stack == null) return;
        
        StatusMessage = $"Restarting stack {stack.Name}...";
        // Create a snapshot to avoid collection modification during iteration
        var containersToRestart = stack.Containers.ToList();
        foreach (var container in containersToRestart)
        {
            await _dockerService.RestartContainerAsync(container.Id);
        }
        StatusMessage = $"Restarted stack {stack.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task RemoveStackAsync(StackViewModel? stack)
    {
        if (stack == null) return;
        
        StatusMessage = $"Removing stack {stack.Name}...";
        
        // Get container IDs from the stack
        var containerIds = stack.Containers.Select(c => c.Id).ToList();
        
        // Use the new RemoveStackAsync method for better handling
        var result = await _dockerService.RemoveStackAsync(stack.Name, containerIds);
        
        StatusMessage = result.IsError 
            ? $"Failed to remove stack {stack.Name}: {result.FirstError.Description}"
            : $"Removed stack {stack.Name}";
            
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private void ShowContainerLogs(ContainerViewModel? container)
    {
        if (container == null || MainWindow == null) return;
        
        _dialogService.ShowLogsWindow(container.Id, container.Name, MainWindow);
    }
    
    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        await _themeService.ToggleThemeAsync();
    }

    [RelayCommand]
    private async Task PruneContainersAsync()
    {
        StatusMessage = "Pruning containers...";
        var result = await _dockerService.PruneContainersAsync();
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : "Containers pruned";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task PruneImagesAsync()
    {
        StatusMessage = "Pruning images...";
        var result = await _dockerService.PruneImagesAsync();
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : "Images pruned";
        await RefreshImagesAsync();
    }

    [RelayCommand]
    private async Task RunImageAsync(ImageViewModel? imageVm)
    {
        if (imageVm == null) return;
        
        try
        {
            // Create and show the dialog
            var dialog = new Views.Dialogs.CreateContainerDialog();
            var dialogViewModel = new ViewModels.Dialogs.CreateContainerDialogViewModel(
                _dockerService,
                _dialogService,
                imageVm.Repository,
                imageVm.Tag);
            
            dialog.DataContext = dialogViewModel;
            
            var desktop = Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
            if (desktop?.MainWindow == null)
            {
                StatusMessage = "Error: Unable to find main window";
                return;
            }
            
            await dialog.ShowDialog(desktop.MainWindow);
            
            if (dialogViewModel.DialogResult == true)
            {
                StatusMessage = $"Container created and started from {imageVm.Repository}:{imageVm.Tag}";
                await RefreshContainersAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error showing dialog: {ex.Message}";
            _logger.LogError(ex, "Error showing create container dialog");
        }
    }

    [RelayCommand]
    private async Task RemoveImageAsync(ImageViewModel? imageVm)
    {
        if (imageVm == null) return;
        
        StatusMessage = $"Removing {imageVm.Repository}:{imageVm.Tag}...";
        var fullImageId = imageVm.FullId ?? imageVm.Id;
        var result = await _dockerService.RemoveImageAsync(fullImageId, force: true);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Removed {imageVm.Repository}:{imageVm.Tag}";
        await RefreshImagesAsync();
    }

    [RelayCommand]
    private async Task PullImageAsync(ImageViewModel? imageVm)
    {
        if (imageVm == null) return;
        
        StatusMessage = $"Pulling {imageVm.Repository}:{imageVm.Tag}...";
        var imageName = $"{imageVm.Repository}:{imageVm.Tag}";
        var result = await _dockerService.PullImageAsync(imageName);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Pulled {imageVm.Repository}:{imageVm.Tag}";
        await RefreshImagesAsync();
    }

    [RelayCommand]
    private async Task PruneVolumesAsync()
    {
        StatusMessage = "Pruning volumes...";
        var result = await _dockerService.PruneVolumesAsync();
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : "Volumes pruned";
        await RefreshVolumesAsync();
    }
    
    [RelayCommand]
    private async Task PruneNetworksAsync()
    {
        StatusMessage = "Pruning networks...";
        var result = await _dockerService.PruneNetworksAsync();
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : "Networks pruned";
        await RefreshNetworksAsync();
    }
    
    [RelayCommand]
    private async Task PruneSystemAsync()
    {
        StatusMessage = "Pruning system...";
        var result = await _dockerService.PruneSystemAsync();
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : "System pruned";
        await RefreshContainersAsync();
        await RefreshImagesAsync();
        await RefreshVolumesAsync();
        await RefreshNetworksAsync();
    }

    [RelayCommand]
    private void ShowContainersView()
    {
        ShowContainers = true;
        ShowImages = false;
        ShowVolumes = false;
        ShowNetworks = false;
        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
        OnPropertyChanged(nameof(VolumesTextColor));
        OnPropertyChanged(nameof(NetworksTextColor));
    }

    [RelayCommand]
    private void ShowImagesView()
    {
        ShowContainers = false;
        ShowImages = true;
        ShowVolumes = false;
        ShowNetworks = false;
        _ = RefreshImagesAsync();
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
        OnPropertyChanged(nameof(VolumesTextColor));
        OnPropertyChanged(nameof(NetworksTextColor));
    }
    
    [RelayCommand]
    private void ShowVolumesView()
    {
        ShowContainers = false;
        ShowImages = false;
        ShowVolumes = true;
        ShowNetworks = false;
        _ = RefreshVolumesAsync();
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
        OnPropertyChanged(nameof(VolumesTextColor));
        OnPropertyChanged(nameof(NetworksTextColor));
    }
    
    [RelayCommand]
    private void ShowNetworksView()
    {
        ShowContainers = false;
        ShowImages = false;
        ShowVolumes = false;
        ShowNetworks = true;
        _ = RefreshNetworksAsync();
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
        OnPropertyChanged(nameof(VolumesTextColor));
        OnPropertyChanged(nameof(NetworksTextColor));
    }

    private async Task RefreshImagesAsync()
    {
        // Try to acquire the semaphore, skip if already refreshing
        if (!await _imageSemaphore.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            var result = await _dockerService.GetImagesAsync();
            
            if (result.IsError)
            {
                StatusMessage = $"Error: {result.FirstError.Description}";
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Images.Clear();
                    foreach (var image in result.Value.OrderBy(i => i.Repository).ThenBy(i => i.Tag))
                    {
                        Images.Add(new ImageViewModel(image));
                    }
                    OnPropertyChanged(nameof(FilteredImages));
                });
            }
        }
        finally
        {
            IsLoading = false;
            _imageSemaphore.Release();
        }
    }

    private async Task RefreshVolumesAsync()
    {
        // Try to acquire the semaphore, skip if already refreshing
        if (!await _volumeSemaphore.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            var result = await _dockerService.GetVolumesAsync();
            
            if (result.IsError)
            {
                StatusMessage = $"Error: {result.FirstError.Description}";
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Volumes.Clear();
                    foreach (var volume in result.Value.OrderBy(v => v.Name))
                    {
                        Volumes.Add(new VolumeViewModel(volume));
                    }
                    OnPropertyChanged(nameof(FilteredVolumes));
                });
            }
        }
        finally
        {
            IsLoading = false;
            _volumeSemaphore.Release();
        }
    }
    
    [RelayCommand]
    private async Task RemoveVolumeAsync(VolumeViewModel? volumeVm)
    {
        if (volumeVm == null) return;
        
        StatusMessage = $"Removing volume {volumeVm.Name}...";
        var result = await _dockerService.RemoveVolumeAsync(volumeVm.Name, force: false);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Removed volume {volumeVm.Name}";
        await RefreshVolumesAsync();
    }

    private async Task RefreshNetworksAsync()
    {
        // Try to acquire the semaphore, skip if already refreshing
        if (!await _networkSemaphore.WaitAsync(0))
            return;

        try
        {
            IsLoading = true;
            var result = await _dockerService.GetNetworksAsync();
            
            if (result.IsError)
            {
                StatusMessage = $"Error: {result.FirstError.Description}";
            }
            else
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Networks.Clear();
                    foreach (var network in result.Value.OrderBy(n => n.Name))
                    {
                        Networks.Add(new NetworkViewModel(network));
                    }
                    OnPropertyChanged(nameof(FilteredNetworks));
                });
            }
        }
        finally
        {
            IsLoading = false;
            _networkSemaphore.Release();
        }
    }
    
    [RelayCommand]
    private async Task RemoveNetworkAsync(NetworkViewModel? networkVm)
    {
        if (networkVm == null) return;
        
        if (networkVm.IsBuiltIn)
        {
            StatusMessage = $"Cannot remove built-in network {networkVm.Name}";
            return;
        }
        
        StatusMessage = $"Removing network {networkVm.Name}...";
        var result = await _dockerService.RemoveNetworkAsync(networkVm.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Removed network {networkVm.Name}";
        await RefreshNetworksAsync();
    }

    private void UpdateContainerList(IEnumerable<ContainerInfo> containers)
    {
        var containerList = containers.ToList();
        
        _containerCache.Edit(cache =>
        {
            var currentIds = new HashSet<string>(containerList.Select(c => c.Id));
            var existingIds = new HashSet<string>(cache.Keys);
            
            var toRemove = existingIds.Except(currentIds).ToList();
            foreach (var id in toRemove)
            {
                var vm = cache.Lookup(id);
                if (vm.HasValue)
                {
                    vm.Value.Dispose();
                    cache.Remove(id);
                }
            }
            
            foreach (var container in containerList)
            {
                var existing = cache.Lookup(container.Id);
                if (existing.HasValue)
                {
                    existing.Value.UpdateFrom(container);
                }
                else
                {
                    cache.AddOrUpdate(new ContainerViewModel(container, _dockerClient));
                }
            }
        });
        
        GroupContainersByStack();

        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(RunningContainersCount));
        OnPropertyChanged(nameof(StoppedContainersCount));
    }
    
    private void GroupContainersByStack()
    {
        if (!Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => GroupContainersByStack());
            return;
        }
        
        var allContainers = Containers.ToList();
        _logger.LogDebug("GroupContainersByStack: {Count} containers total", allContainers.Count);
        
        var stackGroups = allContainers
            .Where(c => c.IsPartOfStack)
            .GroupBy(c => c.StackName)
            .ToList();
        
        var existingStacks = Stacks.ToDictionary(s => s.Name);
        
        Stacks.Clear();
        foreach (var group in stackGroups.OrderBy(g => g.Key))
        {
            var stack = existingStacks.ContainsKey(group.Key!) 
                ? existingStacks[group.Key!] 
                : new StackViewModel(group.Key!);
                
            stack.Containers.Clear();
            foreach (var container in group.OrderBy(c => c.ServiceName).ThenBy(c => c.Name))
            {
                stack.Containers.Add(container);
            }
            
            Stacks.Add(stack);
        }
        
        var standalone = allContainers.Where(c => !c.IsPartOfStack).OrderBy(c => c.Name).ToList();
        _logger.LogDebug("GroupContainersByStack: {Count} standalone containers", standalone.Count);
        
        StandaloneContainers.Clear();
        foreach (var container in standalone)
        {
            StandaloneContainers.Add(container);
        }
    }

    public void Dispose()
    {
        _dockerService?.StopMonitoringEvents();
        _subscriptions?.Dispose();
        
        _containerCache?.Dispose();
        _containerSemaphore?.Dispose();
        _imageSemaphore?.Dispose();
        _volumeSemaphore?.Dispose();
        _networkSemaphore?.Dispose();
        
        foreach (var container in Containers)
        {
            container.Dispose();
        }
    }
}