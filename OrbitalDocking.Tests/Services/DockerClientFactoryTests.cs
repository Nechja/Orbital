using FluentAssertions;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;
using Xunit;

namespace OrbitalDocking.Tests.Services;

public class DockerClientFactoryTests
{
    [Theory]
    [InlineData(null, false, AppConstants.Docker.DefaultDockerEndpoint)]
    [InlineData("", false, AppConstants.Docker.DefaultDockerEndpoint)]
    [InlineData(null, true, AppConstants.Docker.WindowsDockerEndpoint)]
    [InlineData("", true, AppConstants.Docker.WindowsDockerEndpoint)]
    [InlineData("tcp://127.0.0.1:2375", false, "tcp://127.0.0.1:2375")]
    [InlineData("tcp://127.0.0.1:2375", true, "tcp://127.0.0.1:2375")]
    public void GetDockerEndpoint_ResolvesPlatformDefaultUnlessDockerHostIsSet(string? dockerHost, bool isWindows, string expected)
    {
        DockerClientFactory.GetDockerEndpoint(dockerHost, isWindows).Should().Be(expected);
    }
}
