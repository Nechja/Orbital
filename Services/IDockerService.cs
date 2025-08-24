using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDockerService
{
    Task<IEnumerable<ContainerInfo>> GetContainersAsync(CancellationToken cancellationToken = default);
    Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<bool> StartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<bool> RestartContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<bool> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default);
    Task<bool> PauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    Task<bool> UnpauseContainerAsync(string containerId, CancellationToken cancellationToken = default);
    
    Task<IEnumerable<ImageInfo>> GetImagesAsync(CancellationToken cancellationToken = default);
    Task<bool> PullImageAsync(string imageName, IProgress<string>? progress = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveImageAsync(string imageId, bool force = false, CancellationToken cancellationToken = default);
    
    Task<DockerSystemInfo?> GetSystemInfoAsync(CancellationToken cancellationToken = default);
    Task<bool> PruneContainersAsync(CancellationToken cancellationToken = default);
    Task<bool> PruneImagesAsync(CancellationToken cancellationToken = default);
    Task<bool> PruneVolumesAsync(CancellationToken cancellationToken = default);
    
    event EventHandler<ContainerEventArgs>? ContainerEvent;
}

public record ContainerEventArgs(string ContainerId, string Action, DateTime Timestamp);