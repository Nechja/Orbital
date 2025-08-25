using Docker.DotNet;

namespace OrbitalDocking.Services;

public interface IDockerClientFactory
{
    DockerClient CreateClient();
}