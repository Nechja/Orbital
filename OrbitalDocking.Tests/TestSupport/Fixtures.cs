using ErrorOr;
using Moq;
using OrbitalDocking.Models;
using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

public static class Fixtures
{
    public static ContainerInfo Container(string id, string name, ContainerState state = ContainerState.Running, string? stack = null, string image = "nginx:latest")
    {
        var labels = new Dictionary<string, string>();
        if (stack is not null)
        {
            labels["com.docker.compose.project"] = stack;
            labels["com.docker.compose.service"] = name;
        }
        return new ContainerInfo(id, name, image, state, DateTime.UtcNow, state == ContainerState.Running ? "Up" : "Exited", labels, new List<PortMapping>());
    }

    public static ImageInfo Image(string repository, string tag = "latest", long size = 1024 * 1024) =>
        new($"sha256:{repository}{tag}", repository, tag, size, DateTime.UtcNow, "amd64", "linux", new List<string> { $"{repository}:{tag}" }, new Dictionary<string, string>());

    public static DockerSystemInfo SystemInfo(string version = "24.0.7") =>
        new(version, "1.43", "linux", "x86_64", 0, 0, 0, 0, 0, 0, "overlay2");

    /// <summary>A Docker service whose list calls succeed with empty results and whose mutations succeed.</summary>
    public static Mock<IDockerService> DockerService()
    {
        var mock = new Mock<IDockerService>();
        mock.Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<ContainerInfo>>(new List<ContainerInfo>()));
        mock.Setup(x => x.GetImagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<ImageInfo>>(new List<ImageInfo>()));
        mock.Setup(x => x.GetVolumesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<VolumeInfo>>(new List<VolumeInfo>()));
        mock.Setup(x => x.GetNetworksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<NetworkInfo>>(new List<NetworkInfo>()));
        mock.Setup(x => x.GetSystemInfoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From(SystemInfo()));
        mock.Setup(x => x.StartContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        mock.Setup(x => x.StopContainerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        mock.Setup(x => x.RemoveImageAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success);
        return mock;
    }

    public static void ReturnsContainers(this Mock<IDockerService> mock, params ContainerInfo[] containers) =>
        mock.Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<ContainerInfo>>(containers));

    public static void ReturnsImages(this Mock<IDockerService> mock, params ImageInfo[] images) =>
        mock.Setup(x => x.GetImagesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(ErrorOrFactory.From<IEnumerable<ImageInfo>>(images));
}
