using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Concurrency;
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
    private static readonly SynchronizationContextScheduler UiScheduler =
        new(new AvaloniaSynchronizationContext());
    private volatile bool _disposed;

    public Window? MainWindow { get; set; }
    public ITrayService? TrayService { get; private set; }
    public LogsViewModel LogsViewModel { get; }
    
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

        LogsViewModel = new LogsViewModel(dockerClient);
        LogsViewModel.BackRequested += (_, _) => ShowContainersView();

        _themeService.ThemeChanged += OnThemeChanged;
        
        var containers = new ObservableCollectionExtended<ContainerViewModel>();
        _containers = containers;
        _subscriptions.Add(_containerCache.Connect()
            .ObserveOn(UiScheduler)
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
        //TODO gotta be a better way of dealing with this... Hosted service?
        _ = Task.Run(async () => await _dockerService.StartMonitoringEvents());
        
        InitializeTrayService();
    }
    
    private void InitializeTrayService()
    {
        TrayService = new TrayService();
        TrayService.Initialize();
        TrayService.ExitRequested += (_, _) =>
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        };
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

    [ObservableProperty]
    private bool _showSettings = false;

    [ObservableProperty]
    private bool _showLogs = false;

    [ObservableProperty]
    private bool _isSmallScreen = false;

    public bool IsDarkTheme => _themeService.CurrentTheme == ThemeMode.Dark;
    public bool IsLightTheme => _themeService.CurrentTheme == ThemeMode.Light;
    public bool IsSystemTheme => _themeService.CurrentTheme == ThemeMode.System;
    public bool IsHighContrastDarkTheme => _themeService.CurrentTheme == ThemeMode.HighContrastDark;
    public bool IsSoftTheme => _themeService.CurrentTheme == ThemeMode.Soft;
    
    [ObservableProperty]
    private string _dockerEndpoint = AppConstants.Docker.DefaultDockerEndpoint;

    public string ContainersTextColor => ShowContainers ? GetNavigationSelectedColor() : GetSecondaryTextColor();
    public string ImagesTextColor => ShowImages ? GetNavigationSelectedColor() : GetSecondaryTextColor();
    public string VolumesTextColor => ShowVolumes ? GetNavigationSelectedColor() : GetSecondaryTextColor();
    public string NetworksTextColor => ShowNetworks ? GetNavigationSelectedColor() : GetSecondaryTextColor();
    public string LogsTextColor => ShowLogs ? GetNavigationSelectedColor() : GetSecondaryTextColor();
    public string SettingsTextColor => ShowSettings ? GetNavigationSelectedColor() : GetOrbitalTextColor();

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

    partial void OnSearchTextChanged(string value) => NotifyFiltersChanged();

    private void NotifyFiltersChanged()
    {
        OnPropertyChanged(nameof(FilteredContainers));
        OnPropertyChanged(nameof(FilteredImages));
        OnPropertyChanged(nameof(FilteredVolumes));
        OnPropertyChanged(nameof(FilteredNetworks));
    }

    private void NotifyThemeSelectionChanged()
    {
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsLightTheme));
        OnPropertyChanged(nameof(IsSystemTheme));
        OnPropertyChanged(nameof(IsHighContrastDarkTheme));
        OnPropertyChanged(nameof(IsSoftTheme));
    }

    [RelayCommand]
    private async Task RefreshContainersAsync()
    {
        if (_disposed) return;
        try
        {
            if (!await _containerSemaphore.WaitAsync(0))
                return;

            try
            {
                IsLoading = true;
                var result = await _dockerService.GetContainersAsync();
                if (_disposed) return;

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
                if (!_disposed) _containerSemaphore.Release();
            }
        }
        catch (ObjectDisposedException) { }
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
        if (container == null || MainWindow == null) return;

        var containerInfoResult = await _dockerService.GetContainerAsync(container.Id);
        if (containerInfoResult.IsError)
        {
            StatusMessage = $"Failed to get container info: {containerInfoResult.FirstError.Description}";
            return;
        }

        var containerInfo = containerInfoResult.Value;

        var namedVolumes = containerInfo.Volumes?
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .Select(v => v.Name)
            .Distinct()
            .ToList() ?? new List<string>();
        
        var choice = namedVolumes.Count > 0
            ? await _dialogService.ShowVolumeRemovalDialogAsync(container.Name, containerInfo.Volumes!, MainWindow)
            : VolumeRemovalChoice.RemoveContainerOnly;

        if (choice == VolumeRemovalChoice.Cancel)
        {
            StatusMessage = "Container removal cancelled";
            return;
        }

        StatusMessage = $"Removing {container.Name}...";

        var result = await _dockerService.RemoveContainerAsync(container.Id, force: true);
        if (result.IsError)
        {
            StatusMessage = result.ToStatusMessage();
            return;
        }

        if (choice == VolumeRemovalChoice.RemoveContainerAndVolumes && namedVolumes.Count > 0)
        {
            foreach (var volumeName in namedVolumes)
            {
                StatusMessage = $"Removing volume {volumeName}...";
                var volumeResult = await _dockerService.RemoveVolumeAsync(volumeName, force: true);
                if (volumeResult.IsError)
                {
                    _logger.LogWarning("Failed to remove volume {Volume}: {Error}", volumeName, volumeResult.FirstError.Description);
                }
            }
        }

        StatusMessage = choice == VolumeRemovalChoice.RemoveContainerAndVolumes 
            ? $"Removed {container.Name} and its volumes"
            : $"Removed {container.Name}";
            
        await RefreshContainersAsync();
        if (choice == VolumeRemovalChoice.RemoveContainerAndVolumes)
        {
            await RefreshVolumesAsync();
        }
    }
    
    [RelayCommand]
    private Task StartStackAsync(StackViewModel? stack) =>
        RunStackOperationAsync(stack, "Starting", "Started",
            c => !c.IsRunning,
            c => _dockerService.StartContainerAsync(c.Id));

    [RelayCommand]
    private Task StopStackAsync(StackViewModel? stack) =>
        RunStackOperationAsync(stack, "Stopping", "Stopped",
            c => c.IsRunning,
            c => _dockerService.StopContainerAsync(c.Id));

    [RelayCommand]
    private Task RestartStackAsync(StackViewModel? stack) =>
        RunStackOperationAsync(stack, "Restarting", "Restarted",
            _ => true,
            c => _dockerService.RestartContainerAsync(c.Id));

    private async Task RunStackOperationAsync(
        StackViewModel? stack,
        string inProgressVerb,
        string completedVerb,
        Func<ContainerViewModel, bool> predicate,
        Func<ContainerViewModel, Task> operation)
    {
        if (stack is null) return;

        StatusMessage = $"{inProgressVerb} stack {stack.Name}...";
        foreach (var container in stack.Containers.Where(predicate).ToList())
        {
            await operation(container);
        }
        StatusMessage = $"{completedVerb} stack {stack.Name}";
        await RefreshContainersAsync();
    }
    
    [RelayCommand]
    private async Task RemoveStackAsync(StackViewModel? stack)
    {
        if (stack == null || MainWindow == null) return;
        
        // Collect all volumes from all containers in the stack
        var allVolumes = new List<VolumeMount>();
        var containerInfos = new List<ContainerInfo>();
        
        foreach (var container in stack.Containers)
        {
            var containerInfoResult = await _dockerService.GetContainerAsync(container.Id);
            if (containerInfoResult.IsError) continue;
            
            var containerInfo = containerInfoResult.Value;
            containerInfos.Add(containerInfo);
            if (containerInfo.Volumes != null)
            {
                allVolumes.AddRange(containerInfo.Volumes);
            }
        }
        
        var distinctVolumes = allVolumes
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .GroupBy(v => v.Name)
            .Select(g => g.First())
            .ToList();

        var choice = distinctVolumes.Count > 0
            ? await _dialogService.ShowVolumeRemovalDialogAsync($"stack '{stack.Name}'", distinctVolumes, MainWindow)
            : VolumeRemovalChoice.RemoveContainerOnly;

        if (choice == VolumeRemovalChoice.Cancel)
        {
            StatusMessage = "Stack removal cancelled";
            return;
        }
        
        StatusMessage = $"Removing stack {stack.Name}...";

        var containerIds = stack.Containers.Select(c => c.Id).ToList();
        var result = await _dockerService.RemoveStackAsync(stack.Name, containerIds);
        
        if (result.IsError)
        {
            StatusMessage = $"Failed to remove stack {stack.Name}: {result.FirstError.Description}";
            return;
        }
        
        if (choice == VolumeRemovalChoice.RemoveContainerAndVolumes && distinctVolumes.Count > 0)
        {
            var volumeNamesToRemove = distinctVolumes
                .Where(v => !string.IsNullOrEmpty(v.Name))
                .Select(v => v.Name)
                .Distinct()
                .ToList();

            foreach (var volumeName in volumeNamesToRemove)
            {
                StatusMessage = $"Removing volume {volumeName}...";
                var volumeResult = await _dockerService.RemoveVolumeAsync(volumeName, force: true);
                if (volumeResult.IsError)
                {
                    _logger.LogWarning("Failed to remove volume {Volume}: {Error}", volumeName, volumeResult.FirstError.Description);
                }
            }
        }
        
        StatusMessage = choice == VolumeRemovalChoice.RemoveContainerAndVolumes 
            ? $"Removed stack {stack.Name} and its volumes"
            : $"Removed stack {stack.Name}";
            
        await RefreshContainersAsync();
        if (choice == VolumeRemovalChoice.RemoveContainerAndVolumes)
        {
            await RefreshVolumesAsync();
        }
    }

    [RelayCommand]
    private void ShowContainerLogs(ContainerViewModel? container)
    {
        if (container == null) return;

        ShowLogsView();
        LogsViewModel.SelectedContainer = container;
    }
    
    [RelayCommand]
    private async Task ToggleThemeAsync()
    {
        await _themeService.ToggleThemeAsync();
    }
    
    [RelayCommand]
    private Task SetDarkThemeAsync() => ApplyThemeAsync(ThemeMode.Dark, "Theme changed to Dark mode");

    [RelayCommand]
    private Task SetLightThemeAsync() => ApplyThemeAsync(ThemeMode.Light, "Theme changed to Light mode");

    [RelayCommand]
    private Task SetSystemThemeAsync() => ApplyThemeAsync(ThemeMode.System, "Theme set to follow system");

    [RelayCommand]
    private Task SetHighContrastDarkThemeAsync() => ApplyThemeAsync(ThemeMode.HighContrastDark, "Theme changed to High Contrast Dark");

    [RelayCommand]
    private Task SetSoftThemeAsync() => ApplyThemeAsync(ThemeMode.Soft, "Theme changed to Soft");

    private async Task ApplyThemeAsync(ThemeMode mode, string statusMessage)
    {
        await _themeService.SetThemeAsync(mode);
        StatusMessage = statusMessage;
        NotifyThemeSelectionChanged();
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nechja/Orbital",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open GitHub link");
            StatusMessage = "Failed to open GitHub link";
        }
    }
    
    [RelayCommand]
    private void ReportIssue()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/Nechja/Orbital/issues/new",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open issues page");
            StatusMessage = "Failed to open issues page";
        }
    }
    
    [RelayCommand]
    private async Task TestDockerConnectionAsync()
    {
        StatusMessage = "Testing Docker connection...";
        var result = await _dockerService.GetSystemInfoAsync();
        
        if (result.IsError)
        {
            DockerVersion = "Disconnected";
            DockerStatusColor = _themeService.CurrentTheme == ThemeMode.Light ? ThemeColors.Light.DockerOffline : ThemeColors.Dark.DockerOffline;
            StatusMessage = $"Connection failed: {result.FirstError.Description}";
        }
        else
        {
            var systemInfo = result.Value;
            DockerVersion = $"v{systemInfo.ServerVersion}";
            DockerStatusColor = _themeService.CurrentTheme == ThemeMode.Light ? ThemeColors.Light.DockerOnline : ThemeColors.Dark.DockerOnline;
            StatusMessage = $"Connected to Docker v{systemInfo.ServerVersion} - {systemInfo.Containers} containers ({systemInfo.ContainersRunning} running)";
        }
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
        SetActiveView(ActiveView.Containers);
        OnPropertyChanged(nameof(FilteredContainers));
    }

    [RelayCommand]
    private void ShowImagesView()
    {
        SetActiveView(ActiveView.Images);
        _ = RefreshImagesAsync();
    }

    [RelayCommand]
    private void ShowVolumesView()
    {
        SetActiveView(ActiveView.Volumes);
        _ = RefreshVolumesAsync();
    }

    [RelayCommand]
    private void ShowNetworksView()
    {
        SetActiveView(ActiveView.Networks);
        _ = RefreshNetworksAsync();
    }

    [RelayCommand]
    private void ShowLogsView()
    {
        SetActiveView(ActiveView.Logs);

        LogsViewModel.AvailableContainers.Clear();
        foreach (var container in Containers.OrderBy(c => c.Name))
        {
            LogsViewModel.AvailableContainers.Add(container);
        }
    }

    [RelayCommand]
    private void ShowSettingsView() => SetActiveView(ActiveView.Settings);

    private void SetActiveView(ActiveView view)
    {
        ShowContainers = view == ActiveView.Containers;
        ShowImages = view == ActiveView.Images;
        ShowVolumes = view == ActiveView.Volumes;
        ShowNetworks = view == ActiveView.Networks;
        ShowLogs = view == ActiveView.Logs;
        ShowSettings = view == ActiveView.Settings;
        UpdateNavigationColors();
    }

    private enum ActiveView { Containers, Images, Volumes, Networks, Logs, Settings }
    
    private void UpdateNavigationColors()
    {
        OnPropertyChanged(nameof(ContainersTextColor));
        OnPropertyChanged(nameof(ImagesTextColor));
        OnPropertyChanged(nameof(VolumesTextColor));
        OnPropertyChanged(nameof(NetworksTextColor));
        OnPropertyChanged(nameof(LogsTextColor));
        OnPropertyChanged(nameof(SettingsTextColor));
    }

    private async Task RefreshImagesAsync()
    {
        if (_disposed) return;
        try
        {
            if (!await _imageSemaphore.WaitAsync(0))
                return;

            try
            {
                IsLoading = true;
                var result = await _dockerService.GetImagesAsync();
                if (_disposed) return;

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
                if (!_disposed) _imageSemaphore.Release();
            }
        }
        catch (ObjectDisposedException) { }
    }

    private async Task RefreshVolumesAsync()
    {
        if (_disposed) return;
        try
        {
            if (!await _volumeSemaphore.WaitAsync(0))
                return;

            try
            {
                IsLoading = true;
                var result = await _dockerService.GetVolumesAsync();
                if (_disposed) return;

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
                if (!_disposed) _volumeSemaphore.Release();
            }
        }
        catch (ObjectDisposedException) { }
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
        if (_disposed) return;
        try
        {
            if (!await _networkSemaphore.WaitAsync(0))
                return;

            try
            {
                IsLoading = true;
                var result = await _dockerService.GetNetworksAsync();
                if (_disposed) return;

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
                if (!_disposed) _networkSemaphore.Release();
            }
        }
        catch (ObjectDisposedException) { }
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
                    cache.AddOrUpdate(new ContainerViewModel(container, _dockerClient, _themeService));
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

    private string GetPrimaryTextColor()
    {
        return _themeService.CurrentTheme switch
        {
            ThemeMode.Dark => ThemeColors.Dark.TextPrimary,
            ThemeMode.Light => ThemeColors.Light.TextPrimary,
            ThemeMode.HighContrastDark => ThemeColors.HighContrastDark.TextPrimary,
            ThemeMode.Soft => ThemeColors.Soft.TextPrimary,
            _ => ThemeColors.Dark.TextPrimary
        };
    }

    private string GetSecondaryTextColor()
    {
        return _themeService.CurrentTheme switch
        {
            ThemeMode.Dark => ThemeColors.Dark.TextSecondary,
            ThemeMode.Light => ThemeColors.Light.TextSecondary,
            ThemeMode.HighContrastDark => ThemeColors.HighContrastDark.TextSecondary,
            ThemeMode.Soft => ThemeColors.Soft.TextSecondary,
            _ => ThemeColors.Dark.TextSecondary
        };
    }

    private string GetOrbitalTextColor()
    {
        return _themeService.CurrentTheme switch
        {
            ThemeMode.Dark => ThemeColors.Dark.TextSecondary,
            ThemeMode.Light => ThemeColors.Light.TextPrimary,
            ThemeMode.HighContrastDark => ThemeColors.HighContrastDark.TextSecondary,
            ThemeMode.Soft => ThemeColors.Soft.TextPrimary,
            _ => ThemeColors.Dark.TextSecondary
        };
    }

    private string GetNavigationSelectedColor()
    {
        return _themeService.CurrentTheme switch
        {
            ThemeMode.Dark => ThemeColors.Dark.NavigationSelected,
            ThemeMode.Light => ThemeColors.Light.NavigationSelected,
            ThemeMode.HighContrastDark => ThemeColors.HighContrastDark.NavigationSelected,
            ThemeMode.Soft => ThemeColors.Soft.NavigationSelected,
            _ => ThemeColors.Dark.NavigationSelected
        };
    }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        UpdateNavigationColors();
        OnPropertyChanged(nameof(DockerStatusColor));
        NotifyThemeSelectionChanged();

        foreach (var container in Containers)
        {
            container.UpdateThemeColors();
        }

        var isConnected = !string.IsNullOrEmpty(DockerVersion) && DockerVersion != "Disconnected";
        var isLight = _themeService.CurrentTheme == ThemeMode.Light;
        DockerStatusColor = isConnected
            ? (isLight ? ThemeColors.Light.DockerOnline : ThemeColors.Dark.DockerOnline)
            : (isLight ? ThemeColors.Light.DockerOffline : ThemeColors.Dark.DockerOffline);
    }
    
    public void Dispose()
    {
        _disposed = true;
        _themeService.ThemeChanged -= OnThemeChanged;
        _dockerService.StopMonitoringEvents();
        _subscriptions.Dispose();

        TrayService?.Dispose();
        LogsViewModel.Dispose();

        _containerCache.Dispose();
        _containerSemaphore.Dispose();
        _imageSemaphore.Dispose();
        _volumeSemaphore.Dispose();
        _networkSemaphore.Dispose();

        foreach (var container in Containers)
        {
            container.Dispose();
        }
    }
}