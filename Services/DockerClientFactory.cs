using System;
using System.Runtime.InteropServices;
using Docker.DotNet;

namespace OrbitalDocking.Services;

public static class DockerClientFactory
{
    public static DockerClient CreateClient()
    {
        var dockerEndpoint = GetDockerEndpoint();
        return new DockerClientConfiguration(new Uri(dockerEndpoint))
            .CreateClient();
    }

    private static string GetDockerEndpoint()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "npipe://./pipe/docker_engine";
        }
        
        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        if (!string.IsNullOrEmpty(dockerHost))
        {
            return dockerHost;
        }

        return "unix:///var/run/docker.sock";
    }
}