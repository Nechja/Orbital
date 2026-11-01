using Xunit;
using FluentAssertions;
using OrbitalDocking.Models;
using OrbitalDocking.ViewModels;
using System;
using System.Linq;

namespace OrbitalDocking.Tests.GoldenPath;

public class CoreFunctionalityTests
{
    [Fact]
    public void ContainerViewModel_Should_DetermineStateCorrectly()
    {
        var runningContainer = new ContainerInfo(
            Id: "abc123",
            Name: "test",
            Image: "nginx",
            State: ContainerState.Running,
            Created: DateTime.UtcNow,
            Status: "Up",
            Labels: new Dictionary<string, string>(),
            Ports: new List<PortMapping>()
        );

        var vm = new ContainerViewModel(runningContainer);
        
        vm.IsRunning.Should().BeTrue();
        vm.IsStopped.Should().BeFalse();
        vm.CanRemove.Should().BeFalse();
    }

    [Fact]
    public void ContainerViewModel_Should_FormatShortId()
    {
        var container = new ContainerInfo(
            Id: "sha256:1234567890abcdef1234567890abcdef",
            Name: "test",
            Image: "nginx",
            State: ContainerState.Running,
            Created: DateTime.UtcNow,
            Status: "Up",
            Labels: new Dictionary<string, string>(),
            Ports: new List<PortMapping>()
        );

        var vm = new ContainerViewModel(container);
        
        vm.ShortId.Should().Be("sha256:12345");
        vm.ShortId.Length.Should().BeLessThanOrEqualTo(12);
    }

    [Fact]
    public void ImageViewModel_Should_FormatSizeCorrectly()
    {
        var image = new ImageInfo(
            Id: "sha256:abc",
            Repository: "nginx",
            Tag: "latest",
            Size: 1024 * 1024 * 150,
            Created: DateTime.UtcNow,
            Architecture: "amd64",
            OS: "linux",
            RepoTags: new List<string> { "nginx:latest" },
            Labels: new Dictionary<string, string>()
        );

        var vm = new ImageViewModel(image);
        
        vm.SizeFormatted.Should().Contain("MB");
        vm.Repository.Should().Be("nginx");
        vm.Tag.Should().Be("latest");
    }
}
