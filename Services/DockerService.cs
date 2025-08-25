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
            return Result.Success; // Already running is considered success
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
            return Result.Success; // Already stopped is considered success
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


    private void OnContainerEvent(string containerId, string action)
    {
        ContainerEvent?.Invoke(this, new ContainerEventArgs(containerId, action, DateTime.UtcNow));
    }
    
    public void Dispose()
    {
        dockerClient?.Dispose();
    }
}