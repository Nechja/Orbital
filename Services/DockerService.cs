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

public class DockerService(DockerClient dockerClient, IDockerMapper dockerMapper) : IDockerService, IDisposable
{
    public event EventHandler<ContainerEventArgs>? ContainerEvent;

    public async Task<IEnumerable<ContainerInfo>> GetContainersAsync(CancellationToken cancellationToken = default)
    {
        var containers = await dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true },
            cancellationToken);

        return containers.Select(dockerMapper.MapToContainerInfo);
    }

    public async Task<ContainerInfo?> GetContainerAsync(string containerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var container = await dockerClient.Containers.InspectContainerAsync(containerId, cancellationToken);
            return dockerMapper.MapToContainerInfo(container);
        }
        catch (DockerContainerNotFoundException)
        {
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} already running");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error starting container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotModified)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} already stopped");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error stopping container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error restarting container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} is running and cannot be removed");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} is not running");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pausing container {containerId}: {ex.Message}");
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
        catch (DockerContainerNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            System.Diagnostics.Debug.WriteLine($"Container {containerId} is not paused");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error unpausing container {containerId}: {ex.Message}");
            return false;
        }
    }

    public async Task<IEnumerable<ImageInfo>> GetImagesAsync(CancellationToken cancellationToken = default)
    {
        var images = await dockerClient.Images.ListImagesAsync(
            new ImagesListParameters { All = true },
            cancellationToken);

        return images.Select(dockerMapper.MapToImageInfo);
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
        catch (DockerApiException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pulling image {imageName}: {ex.Message}");
            progress?.Report($"Failed to pull image: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Unexpected error pulling image {imageName}: {ex.Message}");
            progress?.Report("Failed to pull image");
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
        catch (DockerImageNotFoundException)
        {
            System.Diagnostics.Debug.WriteLine($"Image {imageId} not found");
            return false;
        }
        catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            System.Diagnostics.Debug.WriteLine($"Image {imageId} is in use by a container");
            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing image {imageId}: {ex.Message}");
            return false;
        }
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
        catch (TimeoutException)
        {
            System.Diagnostics.Debug.WriteLine("Docker daemon not responding");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting Docker system info: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pruning containers: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pruning images: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error pruning volumes: {ex.Message}");
            return false;
        }
    }


    private void OnContainerEvent(string containerId, string action)
    {
        ContainerEvent?.Invoke(this, new ContainerEventArgs(containerId, action, DateTime.UtcNow));
    }
    
    public void Dispose()
    {
        dockerClient?.Dispose();
    }
}