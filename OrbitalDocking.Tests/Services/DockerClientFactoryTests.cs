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

    [Theory]
    [InlineData("/var/run/docker.sock", "unix:///var/run/docker.sock")]
    [InlineData("/Users/kay/.docker/run/docker.sock", "unix:///Users/kay/.docker/run/docker.sock")]
    [InlineData("/Users/kay/.orbstack/run/docker.sock", "unix:///Users/kay/.orbstack/run/docker.sock")]
    [InlineData("/Users/kay/.colima/default/docker.sock", "unix:///Users/kay/.colima/default/docker.sock")]
    [InlineData("/Users/kay/.rd/docker.sock", "unix:///Users/kay/.rd/docker.sock")]
    public void GetDockerEndpoint_UsesTheFirstSocketThatExists(string existing, string expected)
    {
        DockerClientFactory.GetDockerEndpoint(null, false, "/Users/kay", path => path == existing).Should().Be(expected);
    }

    [Fact]
    public void GetDockerEndpoint_PrefersTheSystemSocketWhenSeveralExist()
    {
        var existing = new HashSet<string> { "/var/run/docker.sock", "/Users/kay/.orbstack/run/docker.sock" };

        DockerClientFactory.GetDockerEndpoint(null, false, "/Users/kay", existing.Contains).Should().Be("unix:///var/run/docker.sock");
    }

    [Fact]
    public void GetDockerEndpoint_FallsBackToTheDefaultWhenNoSocketExists()
    {
        DockerClientFactory.GetDockerEndpoint(null, false, "/Users/kay", _ => false).Should().Be(AppConstants.Docker.DefaultDockerEndpoint);
    }

    [Fact]
    public void GetDockerEndpoint_DockerHostWinsOverExistingSockets()
    {
        DockerClientFactory.GetDockerEndpoint("tcp://10.0.0.5:2375", false, "/Users/kay", _ => true).Should().Be("tcp://10.0.0.5:2375");
    }
}
