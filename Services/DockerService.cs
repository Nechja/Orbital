using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using ErrorOr;
using OrbitalDocking.Models;
using OrbitalDocking.Services.Errors;

namespace OrbitalDocking.Services;

public class DockerService(DockerClient dockerClient, IDockerMapper dockerMapper) : IDockerService, IDisposable
{
    public event EventHandler<ContainerEventArgs>? ContainerEvent;
    private CancellationTokenSource? _eventMonitoringCts;
    private Task? _eventMonitoringTask;

    public async Task<ErrorOr<IEnumerable<ContainerInfo>>> GetContainersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containers = await dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true },
                cancellationToken);

            return containers.Select(dockerMapper.MapToContainerInfo).ToList();
        }
        catch (TimeoutException)
        {
            return DockerErrors.Docker.DaemonNotResponding();
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }

    public async Task<ErrorOr<ContainerInfo>> GetContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
            return dockerMapper.MapToContainerInfo(container);
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }

    public async Task<ErrorOr<CreateContainerResponse>> CreateContainerAsync(CreateContainerParameters parameters, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await dockerClient.Containers.CreateContainerAsync(
                parameters,
                cancellationToken);
            
            OnContainerEvent(response.ID, "create");
            return response;
        }
        catch (DockerImageNotFoundException)
        {
            return DockerErrors.Image.NotFound(parameters.Image ?? "unknown");
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Container.AlreadyExists(parameters.Name ?? "unknown");
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }

    public async Task<ErrorOr<Success>> StartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);
            
            OnContainerEvent(containerId, "start");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "start");
        }
    }

    public async Task<ErrorOr<Success>> StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
            
            OnContainerEvent(containerId, "stop");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "stop");
        }
    }

    public async Task<ErrorOr<Success>> RestartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.RestartContainerAsync(
                containerId,
                new ContainerRestartParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
            
            OnContainerEvent(containerId, "restart");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "restart");
        }
    }

    public async Task<ErrorOr<Success>> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = force },
                cancellationToken);
            
            OnContainerEvent(containerId, "remove");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Container.InUse(containerId);
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "remove");
        }
    }

    public async Task<ErrorOr<Success>> RemoveStackAsync(string stackName, IEnumerable<string> containerIds, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerIdList = containerIds.ToList();
            
            // First, stop all running containers in the stack
            var runningContainers = new List<string>();
            foreach (var containerId in containerIdList)
            {
                try
                {
                    var container = await dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
                    if (container.State.Running)
                    {
                        runningContainers.Add(containerId);
                    }
                }
                catch
                {
                    // Container might already be gone, continue
                }
            }
            
            // Stop all running containers in parallel
            if (runningContainers.Any())
            {
                var stopTasks = runningContainers.Select(id => 
                    dockerClient.Containers.StopContainerAsync(id, new ContainerStopParameters { WaitBeforeKillSeconds = 10 }, cancellationToken)
                );
                await Task.WhenAll(stopTasks);
            }
            
            // Now remove all containers in the stack
            var removeTasks = containerIdList.Select(id => 
                dockerClient.Containers.RemoveContainerAsync(id, new ContainerRemoveParameters { Force = true, RemoveVolumes = false }, cancellationToken)
            );
            
            await Task.WhenAll(removeTasks);
            
            // Fire events for each removed container
            foreach (var containerId in containerIdList)
            {
                OnContainerEvent(containerId, "remove");
            }
            
            return Result.Success;
        }
        catch (Exception ex)
        {
            return DockerErrors.Container.OperationFailed(stackName, $"remove stack: {ex.Message}");
        }
    }

    public async Task<ErrorOr<Success>> PauseContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.PauseContainerAsync(containerId, cancellationToken);
            OnContainerEvent(containerId, "pause");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Container.NotRunning(containerId);
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "pause");
        }
    }

    public async Task<ErrorOr<Success>> UnpauseContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.UnpauseContainerAsync(containerId, cancellationToken);
            OnContainerEvent(containerId, "unpause");
            return Result.Success;
        }
        catch (DockerContainerNotFoundException)
        {
            return DockerErrors.Container.NotFound(containerId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Container.NotPaused(containerId);
        }
        catch (Exception)
        {
            return DockerErrors.Container.OperationFailed(containerId, "unpause");
        }
    }

    public async Task<ErrorOr<IEnumerable<ImageInfo>>> GetImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var images = await dockerClient.Images.ListImagesAsync(
                new ImagesListParameters { All = true },
                cancellationToken);

            return images.Select(dockerMapper.MapToImageInfo).ToList();
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }

    public async Task<ErrorOr<Success>> PullImageAsync(string imageName, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var progressHandler = new Progress<JSONMessage>(msg =>
            {
                if (!string.IsNullOrEmpty(msg.Status))
                    progress?.Report(msg.Status);
            });

            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = imageName },
                null,
                progressHandler,
                cancellationToken);

            return Result.Success;
        }
        catch (DockerApiException ex)
        {
            progress?.Report($"Failed to pull image: {ex.Message}");
            return DockerErrors.Image.PullFailed(imageName, "Failed to pull image");
        }
        catch (Exception)
        {
            progress?.Report("Failed to pull image");
            return DockerErrors.Image.PullFailed(imageName, "Failed to pull image");
        }
    }

    public async Task<ErrorOr<Success>> RemoveImageAsync(string imageId, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Images.DeleteImageAsync(
                imageId,
                new ImageDeleteParameters { Force = force },
                cancellationToken);
            return Result.Success;
        }
        catch (DockerImageNotFoundException)
        {
            return DockerErrors.Image.NotFound(imageId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Image.InUse(imageId);
        }
        catch (Exception)
        {
            return DockerErrors.Image.RemoveFailed(imageId);
        }
    }



    public async Task<ErrorOr<DockerSystemInfo>> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var info = await dockerClient.System.GetSystemInfoAsync(cancellationToken);
            return new DockerSystemInfo(
                ServerVersion: info.ServerVersion,
                ApiVersion: string.Empty,
                OS: info.OperatingSystem,
                Architecture: info.Architecture,
                Containers: (int)info.Containers,
                ContainersRunning: (int)info.ContainersRunning,
                ContainersPaused: (int)info.ContainersPaused,
                ContainersStopped: (int)info.ContainersStopped,
                Images: (int)info.Images,
                MemoryTotal: info.MemTotal,
                Driver: info.Driver);
        }
        catch (TimeoutException)
        {
            return DockerErrors.Docker.DaemonNotResponding();
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.ConnectionFailed(ex.Message);
        }
    }

    public async Task<ErrorOr<Success>> PruneContainersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.PruneContainersAsync(cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Prune.ContainersFailed();
        }
    }

    public async Task<ErrorOr<Success>> PruneImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Images.PruneImagesAsync(cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Prune.ImagesFailed();
        }
    }

    public async Task<ErrorOr<Success>> PruneVolumesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Volumes.PruneAsync(cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Prune.VolumesFailed();
        }
    }
    
    public async Task<ErrorOr<Success>> PruneNetworksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Networks.PruneNetworksAsync(cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Prune.NetworksFailed();
        }
    }
    
    public async Task<ErrorOr<Success>> PruneSystemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // System prune is not directly available, so we prune each component
            await dockerClient.Containers.PruneContainersAsync(cancellationToken: cancellationToken);
            await dockerClient.Images.PruneImagesAsync(cancellationToken: cancellationToken);
            await dockerClient.Volumes.PruneAsync(cancellationToken: cancellationToken);
            await dockerClient.Networks.PruneNetworksAsync(cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (Exception)
        {
            return DockerErrors.Prune.SystemFailed();
        }
    }


    public async Task<ErrorOr<IEnumerable<VolumeInfo>>> GetVolumesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var volumeList = await dockerClient.Volumes.ListAsync(cancellationToken);
            return volumeList.Volumes.Select(dockerMapper.MapToVolumeInfo).ToList();
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }
    
    public async Task<ErrorOr<Success>> RemoveVolumeAsync(string volumeName, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Volumes.RemoveAsync(volumeName, force, cancellationToken);
            return Result.Success;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return DockerErrors.Volume.NotFound(volumeName);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Volume.InUse(volumeName);
        }
        catch (Exception)
        {
            return DockerErrors.Volume.RemoveFailed(volumeName);
        }
    }
    
    public async Task<ErrorOr<VolumeInfo>> CreateVolumeAsync(string name, string? driver = null, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new VolumesCreateParameters
            {
                Name = name,
                Driver = driver ?? "local",
                DriverOpts = options
            };
            
            var volume = await dockerClient.Volumes.CreateAsync(parameters, cancellationToken);
            return dockerMapper.MapToVolumeInfo(volume);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Volume.CreateFailed(name, "Volume already exists");
        }
        catch (Exception ex)
        {
            return DockerErrors.Volume.CreateFailed(name, ex.Message);
        }
    }
    
    public async Task<ErrorOr<IEnumerable<NetworkInfo>>> GetNetworksAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var networks = await dockerClient.Networks.ListNetworksAsync(cancellationToken: cancellationToken);
            return networks.Select(dockerMapper.MapToNetworkInfo).ToList();
        }
        catch (Exception ex)
        {
            return DockerErrors.Docker.UnexpectedError(ex.Message);
        }
    }
    
    public async Task<ErrorOr<Success>> RemoveNetworkAsync(string networkId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Networks.DeleteNetworkAsync(networkId, cancellationToken);
            return Result.Success;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return DockerErrors.Network.NotFound(networkId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
        {
            return DockerErrors.Network.BuiltIn(networkId);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Network.InUse(networkId);
        }
        catch (Exception)
        {
            return DockerErrors.Network.RemoveFailed(networkId);
        }
    }
    
    public async Task<ErrorOr<NetworkInfo>> CreateNetworkAsync(string name, string? driver = null, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var parameters = new NetworksCreateParameters
            {
                Name = name,
                Driver = driver ?? "bridge",
                Options = options
            };
            
            var response = await dockerClient.Networks.CreateNetworkAsync(parameters, cancellationToken);
            var networks = await dockerClient.Networks.ListNetworksAsync(new NetworksListParameters
            {
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["id"] = new Dictionary<string, bool> { [response.ID] = true }
                }
            }, cancellationToken);
            
            var network = networks.FirstOrDefault();
            if (network == null)
            {
                return DockerErrors.Network.CreateFailed(name, "Network created but not found");
            }
            
            return dockerMapper.MapToNetworkInfo(network);
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return DockerErrors.Network.CreateFailed(name, "Network already exists");
        }
        catch (Exception ex)
        {
            return DockerErrors.Network.CreateFailed(name, ex.Message);
        }
    }
    
    private void OnContainerEvent(string containerId, string action)
    {
        ContainerEvent?.Invoke(this, new ContainerEventArgs(containerId, action, DateTime.UtcNow));
    }
    
    public async Task StartMonitoringEventsAsync(CancellationToken cancellationToken = default)
    {
        StopMonitoringEvents();
        
        _eventMonitoringCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_eventMonitoringCts.Token, cancellationToken);
        
        _eventMonitoringTask = Task.Run(async () =>
        {
            try
            {
                var parameters = new ContainerEventsParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["type"] = new Dictionary<string, bool> { ["container"] = true }
                    }
                };
                
                var progress = new Progress<Message>(message =>
                {
                    if (message != null && !string.IsNullOrEmpty(message.Action))
                    {
                        Console.WriteLine($"[Docker Event] {message.Action} for container {message.Actor?.ID?.Substring(0, Math.Min(12, message.Actor?.ID?.Length ?? 0))}");
                        
                        if (message.Actor?.ID != null)
                        {
                            OnContainerEvent(message.Actor.ID, message.Action);
                        }
                    }
                });
                
                await dockerClient.System.MonitorEventsAsync(parameters, progress, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Docker Event] Monitoring cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Docker Event] Monitoring error: {ex.Message}");
                
                if (!linkedCts.Token.IsCancellationRequested)
                {
                    await Task.Delay(5000, linkedCts.Token);
                    
                    if (!linkedCts.Token.IsCancellationRequested)
                    {
                        await StartMonitoringEventsAsync(linkedCts.Token);
                    }
                }
            }
        }, linkedCts.Token);
    }
    
    public void StopMonitoringEvents()
    {
        _eventMonitoringCts?.Cancel();
        _eventMonitoringCts?.Dispose();
        _eventMonitoringCts = null;
        _eventMonitoringTask = null;
    }
    
    public void Dispose()
    {
        StopMonitoringEvents();
        dockerClient?.Dispose();
    }
}