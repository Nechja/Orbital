using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
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
    private readonly Timer _refreshTimer;
    private readonly Timer _imageRefreshTimer;
    private readonly Timer _volumeRefreshTimer;
    private readonly Timer _networkRefreshTimer;

    public Window? MainWindow { get; set; }
    
    public MainWindowViewModel(
        IDockerService dockerService, 
        IThemeService themeService, 
        DockerClient dockerClient, 
        IDialogService dialogService)
    {
        _dockerService = dockerService;
        _themeService = themeService;
        _dockerClient = dockerClient;
        _dialogService = dialogService;
        
        _refreshTimer = new Timer(async _ => await RefreshContainersAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        _imageRefreshTimer = new Timer(async _ => await RefreshImagesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        _volumeRefreshTimer = new Timer(async _ => await RefreshVolumesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        _networkRefreshTimer = new Timer(async _ => await RefreshNetworksAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
        
        _dockerService.ContainerEvent += OnContainerEvent;
        _ = GetDockerVersionAsync();
    }
    
    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _containers = new();
    
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
    private string _dockerStatusColor = "#666688";

    public int RunningContainersCount => Containers.Count(c => c.IsRunning);
    public int StoppedContainersCount => Containers.Count(c => c.IsStopped);


    private async Task GetDockerVersionAsync()
    {
        var result = await _dockerService.GetSystemInfoAsync();
        
        if (result.IsError)
        {
            DockerVersion = "Disconnected";
            DockerStatusColor = "#FF6B6B";
            StatusMessage = result.FirstError.Description;
        }
        else
        {
            var systemInfo = result.Value;
            DockerVersion = $"v{systemInfo.ServerVersion}";
            DockerStatusColor = "#4ECDC4";
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
        IsLoading = true;
        var result = await _dockerService.GetContainersAsync();
        
        if (result.IsError)
        {
            StatusMessage = $"Error: {result.FirstError.Description}";
        }
        else
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateContainerList(result.Value);
            });
        }
        
        IsLoading = false;
    }

    [RelayCommand]
    private async Task StartContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Starting {SelectedContainer.Name}...";
        var result = await _dockerService.StartContainerAsync(SelectedContainer.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Started {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task StopContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Stopping {SelectedContainer.Name}...";
        var result = await _dockerService.StopContainerAsync(SelectedContainer.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Stopped {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RestartContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Restarting {SelectedContainer.Name}...";
        var result = await _dockerService.RestartContainerAsync(SelectedContainer.Id);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Restarted {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RemoveContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Removing {SelectedContainer.Name}...";
        var result = await _dockerService.RemoveContainerAsync(SelectedContainer.Id, force: true);
        StatusMessage = result.IsError 
            ? result.ToStatusMessage()
            : $"Removed {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task StartStackAsync(StackViewModel? stack)
    {
        if (stack == null) return;
        
        StatusMessage = $"Starting stack {stack.Name}...";
        foreach (var container in stack.Containers.Where(c => !c.IsRunning))
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
        foreach (var container in stack.Containers.Where(c => c.IsRunning))
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
        foreach (var container in stack.Containers)
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
        foreach (var container in stack.Containers)
        {
            await _dockerService.RemoveContainerAsync(container.Id, force: true);
        }
        StatusMessage = $"Removed stack {stack.Name}";
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
        
        IsLoading = false;
    }

    private async Task RefreshVolumesAsync()
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
            });
        }
        
        IsLoading = false;
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
            });
        }
        
        IsLoading = false;
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
        
        foreach (var container in containerList)
        {
            var existingVm = Containers.FirstOrDefault(c => c.Id == container.Id);
            if (existingVm != null)
            {
                existingVm.UpdateFrom(container);
            }
            else
            {
                Containers.Add(new ContainerViewModel(container, _dockerClient));
            }
        }

        var toRemove = Containers.Where(c => !containerList.Any(nc => nc.Id == c.Id)).ToList();
        foreach (var container in toRemove)
        {
            Containers.Remove(container);
            container.Dispose();
        }
        
        GroupContainersByStack();

        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(RunningContainersCount));
        OnPropertyChanged(nameof(StoppedContainersCount));
    }
    
    private void GroupContainersByStack()
    {
        var stackGroups = Containers
            .Where(c => c.IsPartOfStack)
            .GroupBy(c => c.StackName)
            .ToList();
        
        // Keep existing stacks to preserve their expanded state
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
        
        StandaloneContainers.Clear();
        foreach (var container in Containers.Where(c => !c.IsPartOfStack).OrderBy(c => c.Name))
        {
            StandaloneContainers.Add(container);
        }
    }

    private void OnContainerEvent(object? sender, ContainerEventArgs e)
    {
        StatusMessage = $"Container {e.Action}: {e.ContainerId}";
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _imageRefreshTimer?.Dispose();
        _volumeRefreshTimer?.Dispose();
        _networkRefreshTimer?.Dispose();
        _dockerService.ContainerEvent -= OnContainerEvent;
        
        foreach (var container in Containers)
        {
            container.Dispose();
        }
    }
}