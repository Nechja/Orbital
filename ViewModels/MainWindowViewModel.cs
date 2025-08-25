using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Docker.DotNet;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IThemeService _themeService;
    private readonly DockerClient _dockerClient;
    private readonly Timer _refreshTimer;
    private readonly Timer _imageRefreshTimer;

    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _containers = new();

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

    public string ContainersTextColor => ShowContainers ? "#FFFFFF" : "#8888AA";
    public string ImagesTextColor => ShowImages ? "#FFFFFF" : "#8888AA";

    [ObservableProperty]
    private string _dockerVersion = "Connecting...";

    [ObservableProperty]
    private string _dockerStatusColor = "#666688";

    public int RunningContainersCount => Containers.Count(c => c.IsRunning);
    public int StoppedContainersCount => Containers.Count(c => c.IsStopped);

    public MainWindowViewModel(IDockerService dockerService, IThemeService themeService, DockerClient dockerClient)
    {
        _dockerService = dockerService;
        _themeService = themeService;
        _dockerClient = dockerClient;
        _refreshTimer = new Timer(async _ => await RefreshContainersAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
        _imageRefreshTimer = new Timer(async _ => await RefreshImagesAsync(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        
        _dockerService.ContainerEvent += OnContainerEvent;
        
        _ = GetDockerVersionAsync();
    }

    private async Task GetDockerVersionAsync()
    {
        try
        {
            var systemInfo = await _dockerService.GetSystemInfoAsync();
            if (systemInfo != null)
            {
                DockerVersion = $"v{systemInfo.ServerVersion}";
                DockerStatusColor = "#4ECDC4";
                StatusMessage = "Connected to Docker";
            }
            else
            {
                DockerVersion = "Disconnected";
                DockerStatusColor = "#FF6B6B";
                StatusMessage = "Docker daemon not responding";
            }
        }
        catch (Exception)
        {
            DockerVersion = "Error";
            DockerStatusColor = "#FF6B6B";
            StatusMessage = "Failed to connect to Docker";
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

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(FilteredImages));
    }

    [RelayCommand]
    private async Task RefreshContainersAsync()
    {
        try
        {
            IsLoading = true;
            var containers = await _dockerService.GetContainersAsync();
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateContainerList(containers);
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Starting {SelectedContainer.Name}...";
        var success = await _dockerService.StartContainerAsync(SelectedContainer.Id);
        StatusMessage = success ? $"Started {SelectedContainer.Name}" : $"Failed to start {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task StopContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Stopping {SelectedContainer.Name}...";
        var success = await _dockerService.StopContainerAsync(SelectedContainer.Id);
        StatusMessage = success ? $"Stopped {SelectedContainer.Name}" : $"Failed to stop {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RestartContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Restarting {SelectedContainer.Name}...";
        var success = await _dockerService.RestartContainerAsync(SelectedContainer.Id);
        StatusMessage = success ? $"Restarted {SelectedContainer.Name}" : $"Failed to restart {SelectedContainer.Name}";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task RemoveContainerAsync()
    {
        if (SelectedContainer == null) return;

        StatusMessage = $"Removing {SelectedContainer.Name}...";
        var success = await _dockerService.RemoveContainerAsync(SelectedContainer.Id, force: true);
        StatusMessage = success ? $"Removed {SelectedContainer.Name}" : $"Failed to remove {SelectedContainer.Name}";
        await RefreshContainersAsync();
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
        var success = await _dockerService.PruneContainersAsync();
        StatusMessage = success ? "Containers pruned" : "Failed to prune containers";
        await RefreshContainersAsync();
    }

    [RelayCommand]
    private async Task PruneImagesAsync()
    {
        StatusMessage = "Pruning images...";
        var success = await _dockerService.PruneImagesAsync();
        StatusMessage = success ? "Images pruned" : "Failed to prune images";
        await RefreshImagesAsync();
    }

    [RelayCommand]
    private async Task RemoveImageAsync(ImageViewModel? imageVm)
    {
        if (imageVm == null) return;
        
        StatusMessage = $"Removing {imageVm.Repository}:{imageVm.Tag}...";
        var fullImageId = imageVm.FullId ?? imageVm.Id;
        var success = await _dockerService.RemoveImageAsync(fullImageId, force: true);
        StatusMessage = success ? $"Removed {imageVm.Repository}:{imageVm.Tag}" : $"Failed to remove {imageVm.Repository}:{imageVm.Tag}";
        await RefreshImagesAsync();
    }

    [RelayCommand]
    private async Task PruneVolumesAsync()
    {
        StatusMessage = "Pruning volumes...";
        var success = await _dockerService.PruneVolumesAsync();
        StatusMessage = success ? "Volumes pruned" : "Failed to prune volumes";
        await RefreshVolumesAsync();
    }

    [RelayCommand]
    private void ShowContainersView()
    {
        ShowContainers = true;
        ShowImages = false;
        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
    }

    [RelayCommand]
    private void ShowImagesView()
    {
        ShowContainers = false;
        ShowImages = true;
        _ = RefreshImagesAsync();
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
    }

    private async Task RefreshImagesAsync()
    {
        try
        {
            IsLoading = true;
            var images = await _dockerService.GetImagesAsync();
            
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Images.Clear();
                foreach (var image in images)
                {
                    Images.Add(new ImageViewModel(image));
                }
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshVolumesAsync()
    {
        // Volume management not implemented yet
        await Task.CompletedTask;
    }

    private async Task RefreshNetworksAsync()
    {
        // Network management not implemented yet
        await Task.CompletedTask;
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

        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(RunningContainersCount));
        OnPropertyChanged(nameof(StoppedContainersCount));
    }

    private void OnContainerEvent(object? sender, ContainerEventArgs e)
    {
        StatusMessage = $"Container {e.Action}: {e.ContainerId}";
    }

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _imageRefreshTimer?.Dispose();
        _dockerService.ContainerEvent -= OnContainerEvent;
        
        foreach (var container in Containers)
        {
            container.Dispose();
        }
    }
}