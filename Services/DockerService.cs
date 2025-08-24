using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public class DockerService(DockerClient dockerClient) : IDockerService
{
    public event EventHandler<ContainerEventArgs>? ContainerEvent;

    public async Task<IEnumerable<ContainerInfo>> GetContainersAsync(CancellationToken cancellationToken = default)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken);

        return containers.Select(MapToContainerInfo);
    }

    public async Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
            return MapToContainerInfo(container);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> StartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var started = await dockerClient.Containers.StartContainerAsync(
                containerId,
                new ContainerStartParameters(),
                cancellationToken);
            
            OnContainerEvent(containerId, "start");
            return started;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> StopContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var stopped = await dockerClient.Containers.StopContainerAsync(
                containerId,
                new ContainerStopParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
            
            OnContainerEvent(containerId, "stop");
            return stopped;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestartContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.RestartContainerAsync(
                containerId,
                new ContainerRestartParameters { WaitBeforeKillSeconds = 10 },
                cancellationToken);
            
            OnContainerEvent(containerId, "restart");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveContainerAsync(string containerId, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.RemoveContainerAsync(
                containerId,
                new ContainerRemoveParameters { Force = force },
                cancellationToken);
            
            OnContainerEvent(containerId, "remove");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PauseContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.PauseContainerAsync(containerId, cancellationToken);
            OnContainerEvent(containerId, "pause");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnpauseContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.UnpauseContainerAsync(containerId, cancellationToken);
            OnContainerEvent(containerId, "unpause");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<ImageInfo>> GetImagesAsync(CancellationToken cancellationToken = default)
    {
        var images = await dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = true },
            cancellationToken);

        return images.Select(MapToImageInfo);
    }

    public async Task<bool> PullImageAsync(string imageName, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
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

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveImageAsync(string imageId, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Images.DeleteImageAsync(
                imageId,
                new ImageDeleteParameters { Force = force },
                cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<VolumeInfo>> GetVolumesAsync(CancellationToken cancellationToken = default)
    {
        var response = await dockerClient.Volumes.ListAsync(cancellationToken);
        return response.Volumes.Select(MapToVolumeInfo);
    }

    public async Task<VolumeInfo?> CreateVolumeAsync(string name, Dictionary<string, string>? labels = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var volume = await dockerClient.Volumes.CreateAsync(
                new VolumesCreateParameters
                {
                    Name = name,
                    Labels = labels ?? new Dictionary<string, string>()
                },
                cancellationToken);

            return MapToVolumeInfo(volume);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RemoveVolumeAsync(string name, bool force = false, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Volumes.RemoveAsync(name, force, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<NetworkInfo>> GetNetworksAsync(CancellationToken cancellationToken = default)
    {
        var networks = await dockerClient.Networks.ListNetworksAsync(cancellationToken: cancellationToken);
        return networks.Select(MapToNetworkInfo);
    }

    public async Task<NetworkInfo?> CreateNetworkAsync(string name, string driver = "bridge", CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await dockerClient.Networks.CreateNetworkAsync(
                new NetworksCreateParameters
                {
                    Name = name,
                    Driver = driver
                },
                cancellationToken);

            var network = await dockerClient.Networks.InspectNetworkAsync(response.ID, cancellationToken);
            return MapToNetworkInfo(network);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> RemoveNetworkAsync(string networkId, CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Networks.DeleteNetworkAsync(networkId, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ContainerStats?> GetContainerStatsAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var statsStream = await dockerClient.Containers.GetContainerStatsAsync(
                containerId,
                new ContainerStatsParameters { Stream = false },
                cancellationToken);

            using var reader = new System.IO.StreamReader(statsStream);
            var statsJson = await reader.ReadToEndAsync(cancellationToken);
            
            return new ContainerStats(
                CpuPercent: 0,
                MemoryUsage: 0,
                MemoryLimit: 0,
                MemoryPercent: 0,
                NetworkRx: 0,
                NetworkTx: 0,
                BlockRead: 0,
                BlockWrite: 0,
                Timestamp: DateTime.UtcNow);
        }
        catch
        {
            return null;
        }
    }

    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string containerId,
        bool follow = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new LogEntry(
            DateTime.UtcNow,
            "Log streaming not yet implemented",
            LogLevel.Info,
            "system");
    }

    public async Task<DockerSystemInfo?> GetSystemInfoAsync(CancellationToken cancellationToken = default)
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
        catch
        {
            return null;
        }
    }

    public async Task<bool> PruneContainersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Containers.PruneContainersAsync(cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PruneImagesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Images.PruneImagesAsync(cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> PruneVolumesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dockerClient.Volumes.PruneAsync(cancellationToken: cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ContainerInfo MapToContainerInfo(ContainerListResponse container)
    {
        return new ContainerInfo(
            Id: container.ID,
            Name: container.Names.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
            Image: container.Image,
            State: ParseContainerState(container.State),
            Created: container.Created,
            Status: container.Status,
            Labels: container.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Ports: container.Ports?.Select(p => new PortMapping(
                p.PrivatePort.ToString(),
                p.PublicPort.ToString(),
                p.Type,
                p.IP ?? string.Empty)).ToList() ?? new List<PortMapping>());
    }

    private static ContainerInfo MapToContainerInfo(ContainerInspectResponse container)
    {
        return new ContainerInfo(
            Id: container.ID,
            Name: container.Name.TrimStart('/'),
            Image: container.Image,
            State: ParseContainerState(container.State.Status),
            Created: container.Created,
            Status: container.State.Status,
            Labels: container.Config.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Ports: new List<PortMapping>());
    }

    private static ImageInfo MapToImageInfo(ImagesListResponse image)
    {
        return new ImageInfo(
            Id: image.ID,
            Repository: image.RepoTags?.FirstOrDefault()?.Split(':')[0] ?? "<none>",
            Tag: image.RepoTags?.FirstOrDefault()?.Split(':').LastOrDefault() ?? "<none>",
            Size: image.Size,
            Created: image.Created,
            Architecture: string.Empty,
            OS: string.Empty,
            RepoTags: image.RepoTags?.ToList() ?? new List<string>(),
            Labels: image.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    private static VolumeInfo MapToVolumeInfo(VolumeResponse volume)
    {
        return new VolumeInfo(
            Name: volume.Name,
            Driver: volume.Driver,
            MountPoint: volume.Mountpoint,
            Created: DateTime.TryParse(volume.CreatedAt, out var created) ? created : DateTime.MinValue,
            Labels: volume.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Options: volume.Options?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    private static NetworkInfo MapToNetworkInfo(NetworkResponse network)
    {
        return new NetworkInfo(
            Id: network.ID,
            Name: network.Name,
            Driver: network.Driver,
            Scope: network.Scope,
            Internal: network.Internal,
            Attachable: network.Attachable,
            Created: network.Created,
            Options: network.Options?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    private static Models.ContainerState ParseContainerState(string state)
    {
        return state?.ToLowerInvariant() switch
        {
            "running" => Models.ContainerState.Running,
            "paused" => Models.ContainerState.Paused,
            "exited" => Models.ContainerState.Exited,
            "created" => Models.ContainerState.Created,
            "restarting" => Models.ContainerState.Restarting,
            "dead" => Models.ContainerState.Dead,
            "removing" => Models.ContainerState.Removing,
            _ => Models.ContainerState.Exited
        };
    }

    private void OnContainerEvent(string containerId, string action)
    {
        ContainerEvent?.Invoke(this, new ContainerEventArgs(containerId, action, DateTime.UtcNow));
    }
}