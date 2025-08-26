using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Services;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.ViewModels.Dialogs;

public partial class CreateContainerDialogViewModel : ViewModelBase
{
    private readonly IDockerService _dockerService;
    private readonly IDialogService _dialogService;
    private readonly string _imageName;
    private readonly string _imageTag;

    [ObservableProperty]
    private string _containerName = "";

    [ObservableProperty]
    private string _imageFullName = "";

    [ObservableProperty]
    private ObservableCollection<PortMappingViewModel> _portMappings = new();

    [ObservableProperty]
    private ObservableCollection<EnvironmentVariableViewModel> _environmentVariables = new();

    [ObservableProperty]
    private string _nameValidationError = "";

    [ObservableProperty]
    private bool _hasNameError;

    [ObservableProperty]
    private string _validationSummary = "";

    [ObservableProperty]
    private bool _canCreate = true;

    public bool? DialogResult { get; private set; }
    public string? CreatedContainerId { get; private set; }

    public CreateContainerDialogViewModel(
        IDockerService dockerService,
        IDialogService dialogService,
        string imageName,
        string imageTag)
    {
        _dockerService = dockerService;
        _dialogService = dialogService;
        _imageName = imageName;
        _imageTag = imageTag;
        
        ImageFullName = $"{imageName}:{imageTag}";
        
        // Generate initial container name
        GenerateName();
        
        // TODO: Auto-detect exposed ports from image
    }

    [RelayCommand]
    private void GenerateName()
    {
        // Generate a unique name based on image and timestamp
        var baseName = _imageName.Replace("/", "-").Replace(":", "-");
        var timestamp = DateTime.Now.ToString("MMdd-HHmmss");
        ContainerName = $"{baseName}-{timestamp}";
        _ = ValidateContainerName();
    }

    [RelayCommand]
    private void AddPort()
    {
        PortMappings.Add(new PortMappingViewModel());
    }

    [RelayCommand]
    private void RemovePort(PortMappingViewModel port)
    {
        PortMappings.Remove(port);
    }

    [RelayCommand]
    private void AddEnvironmentVariable()
    {
        EnvironmentVariables.Add(new EnvironmentVariableViewModel());
    }

    [RelayCommand]
    private void RemoveEnvironmentVariable(EnvironmentVariableViewModel variable)
    {
        EnvironmentVariables.Remove(variable);
    }

    [RelayCommand]
    private async Task CreateAndStart()
    {
        if (!await ValidateAll())
            return;

        try
        {
            // Build port bindings dictionary
            var portBindings = new Dictionary<string, IList<Docker.DotNet.Models.PortBinding>>();
            foreach (var port in PortMappings.Where(p => !string.IsNullOrWhiteSpace(p.ContainerPort)))
            {
                var key = $"{port.ContainerPort}/{port.Protocol}";
                portBindings[key] = new List<Docker.DotNet.Models.PortBinding>
                {
                    new Docker.DotNet.Models.PortBinding
                    {
                        HostPort = port.HostPort
                    }
                };
            }

            // Build environment variables
            var envVars = EnvironmentVariables
                .Where(e => !string.IsNullOrWhiteSpace(e.Key))
                .Select(e => $"{e.Key}={e.Value}")
                .ToList();

            var createParams = new Docker.DotNet.Models.CreateContainerParameters
            {
                Name = ContainerName,
                Image = ImageFullName,
                Env = envVars,
                HostConfig = new Docker.DotNet.Models.HostConfig
                {
                    PortBindings = portBindings
                }
            };

            // Create the container
            var result = await _dockerService.CreateContainerAsync(createParams);
            
            if (result.IsError)
            {
                ValidationSummary = $"Failed to create container: {result.FirstError.Description}";
                return;
            }

            CreatedContainerId = result.Value.ID;

            // Start the container
            var startResult = await _dockerService.StartContainerAsync(CreatedContainerId);
            if (startResult.IsError)
            {
                ValidationSummary = $"Container created but failed to start: {startResult.FirstError.Description}";
                return;
            }

            DialogResult = true;
            CloseDialog();
        }
        catch (Exception ex)
        {
            ValidationSummary = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        CloseDialog();
    }

    private void CloseDialog()
    {
        // The dialog service will handle closing
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close();
        }
    }

    private async Task<bool> ValidateAll()
    {
        ValidationSummary = "";
        
        // Validate container name
        if (!await ValidateContainerName())
            return false;

        // Validate at least one port mapping if any are specified
        var hasValidPort = !PortMappings.Any() || 
                          PortMappings.Any(p => !string.IsNullOrWhiteSpace(p.ContainerPort));
        
        if (!hasValidPort)
        {
            ValidationSummary = "Please specify at least one valid port mapping or remove all mappings";
            return false;
        }

        // Check for port conflicts
        foreach (var port in PortMappings.Where(p => !string.IsNullOrWhiteSpace(p.HostPort)))
        {
            if (!int.TryParse(port.HostPort, out var portNum) || portNum < 1 || portNum > 65535)
            {
                ValidationSummary = $"Invalid host port: {port.HostPort}";
                return false;
            }

            // TODO: Check if port is already in use
        }

        return true;
    }

    private async Task<bool> ValidateContainerName()
    {
        if (string.IsNullOrWhiteSpace(ContainerName))
        {
            NameValidationError = "Container name is required";
            HasNameError = true;
            return false;
        }

        // Check if name already exists
        var containers = await _dockerService.GetContainersAsync();
        if (!containers.IsError && containers.Value.Any(c => c.Name == ContainerName))
        {
            NameValidationError = "A container with this name already exists";
            HasNameError = true;
            return false;
        }

        NameValidationError = "";
        HasNameError = false;
        return true;
    }

    partial void OnContainerNameChanged(string value)
    {
        _ = ValidateContainerName();
    }
}