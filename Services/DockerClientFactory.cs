using System;
using System.Collections.Generic;
using System.IO;
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
        GetDockerEndpoint(
            Environment.GetEnvironmentVariable("DOCKER_HOST"),
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            File.Exists);

    public static string GetDockerEndpoint(string? dockerHost, bool isWindows) =>
        GetDockerEndpoint(dockerHost, isWindows, string.Empty, _ => false);

    public static string GetDockerEndpoint(string? dockerHost, bool isWindows, string home, Func<string, bool> socketExists)
    {
        if (!string.IsNullOrWhiteSpace(dockerHost))
        {
            return dockerHost;
        }

        if (isWindows)
        {
            return AppConstants.Docker.WindowsDockerEndpoint;
        }

        foreach (var socket in CandidateSockets(home))
        {
            if (socketExists(socket))
            {
                return "unix://" + socket;
            }
        }

        return AppConstants.Docker.DefaultDockerEndpoint;
    }

    public static IEnumerable<string> CandidateSockets(string home)
    {
        var root = home.TrimEnd('/');
        yield return "/var/run/docker.sock";
        yield return root + "/.docker/run/docker.sock";
        yield return root + "/.orbstack/run/docker.sock";
        yield return root + "/.colima/default/docker.sock";
        yield return root + "/.rd/docker.sock";
    }
}
