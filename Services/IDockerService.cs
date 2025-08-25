using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDockerService
{
    Task<ErrorOr<IEnumerable<ContainerInfo>>> GetContainersAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<ContainerInfo>> GetContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> StartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> StopContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> UnpauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    
    Task<ErrorOr<IEnumerable<ImageInfo>>> GetImagesAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PullImageAsync(string imageName, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> RemoveImageAsync(string imageId, bool force = false, CancellationToken cancellationToken = default);
    
    Task<ErrorOr<DockerSystemInfo>> GetSystemInfoAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PruneContainersAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PruneImagesAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PruneVolumesAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PruneNetworksAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> PruneSystemAsync(CancellationToken cancellationToken = default);
    
    Task<ErrorOr<IEnumerable<VolumeInfo>>> GetVolumesAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> RemoveVolumeAsync(string volumeName, bool force = false, CancellationToken cancellationToken = default);
    Task<ErrorOr<VolumeInfo>> CreateVolumeAsync(string name, string? driver = null, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default);
    
    Task<ErrorOr<IEnumerable<NetworkInfo>>> GetNetworksAsync(CancellationToken cancellationToken = default);
    Task<ErrorOr<Success>> RemoveNetworkAsync(string networkId, CancellationToken cancellationToken = default);
    Task<ErrorOr<NetworkInfo>> CreateNetworkAsync(string name, string? driver = null, Dictionary<string, string>? options = null, CancellationToken cancellationToken = default);
    
    event EventHandler<ContainerEventArgs>? ContainerEvent;
}

public record ContainerEventArgs(string ContainerId, string Action, DateTime Timestamp);