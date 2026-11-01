using System;
using System.Runtime.InteropServices;
using Docker.DotNet;
using OrbitalDocking.Configuration;

namespace OrbitalDocking.Services;

public class DockerClientFactory : IDockerClientFactory
{
    public DockerClient CreateClient()
    {
        var dockerEndpoint = GetDockerEndpoint();
        return new DockerClientConfiguration(new Uri(dockerEndpoint))
            .CreateClient();
    }

    public static string GetDockerEndpoint() =>
        GetDockerEndpoint(Environment.GetEnvironmentVariable("DOCKER_HOST"), RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

    public static string GetDockerEndpoint(string? dockerHost, bool isWindows)
    {
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            return dockerHost;
        }

        return isWindows
            ? AppConstants.Docker.WindowsDockerEndpoint
            : AppConstants.Docker.DefaultDockerEndpoint;
    }
}
