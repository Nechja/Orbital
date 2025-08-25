using Xunit;
using FluentAssertions;
using Moq;
using Docker.DotNet;
using OrbitalDocking.Models;
using OrbitalDocking.Services;
using OrbitalDocking.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrbitalDocking.Tests.GoldenPath;

public class ViewModelTests
{
    private readonly Mock<IDockerService> _dockerServiceMock;
    private readonly Mock<IThemeService> _themeServiceMock;
    private readonly Mock<IDialogService> _dialogServiceMock;
    private readonly DockerClient? _dockerClient = null;

    public ViewModelTests()
    {
        _dockerServiceMock = new Mock<IDockerService>();
        _themeServiceMock = new Mock<IThemeService>();
        _dialogServiceMock = new Mock<IDialogService>();
    }

    [Fact]
    public async Task MainWindowViewModel_Should_LoadContainers()
    {
        var containers = new List<ContainerInfo>
        {
            new ContainerInfo(
                Id: "container1",
                Name: "nginx",
                Image: "nginx:latest",
                State: ContainerState.Running,
                Created: DateTime.UtcNow,
                Status: "Up 5 minutes",
                Labels: new Dictionary<string, string>(),
                Ports: new List<PortMapping>()
            ),
            new ContainerInfo(
                Id: "container2",
                Name: "redis",
                Image: "redis:alpine",
                State: ContainerState.Exited,
                Created: DateTime.UtcNow,
                Status: "Exited",
                Labels: new Dictionary<string, string>(),
                Ports: new List<PortMapping>()
            )
        };

        _dockerServiceMock
            .Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(containers);

        var vm = new MainWindowViewModel(_dockerServiceMock.Object, _themeServiceMock.Object, _dockerClient!, _dialogServiceMock.Object);
        
        // Wait for the timer to trigger
        await Task.Delay(100);

        // Verify the service was called
        _dockerServiceMock.Verify(x => x.GetContainersAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        
        vm.Dispose();
    }

    [Fact]
    public void MainWindowViewModel_Should_FilterContainers()
    {
        _dockerServiceMock
            .Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerInfo>());

        var vm = new MainWindowViewModel(_dockerServiceMock.Object, _themeServiceMock.Object, _dockerClient!, _dialogServiceMock.Object);

        // Manually add containers for testing
        vm.Containers.Add(new ContainerViewModel(new ContainerInfo(
            Id: "abc123",
            Name: "nginx-test",
            Image: "nginx:latest",
            State: ContainerState.Running,
            Created: DateTime.UtcNow,
            Status: "Up",
            Labels: new Dictionary<string, string>(),
            Ports: new List<PortMapping>()
        )));
        
        vm.Containers.Add(new ContainerViewModel(new ContainerInfo(
            Id: "def456",
            Name: "redis-cache",
            Image: "redis:alpine",
            State: ContainerState.Running,
            Created: DateTime.UtcNow,
            Status: "Up",
            Labels: new Dictionary<string, string>(),
            Ports: new List<PortMapping>()
        )));

        vm.SearchText = "nginx";
        vm.FilteredContainers.Should().HaveCount(1);
        vm.FilteredContainers.First().Name.Should().Contain("nginx");

        vm.SearchText = "";
        vm.FilteredContainers.Should().HaveCount(2);
        
        vm.Dispose();
    }

    [Fact]
    public void MainWindowViewModel_Should_SwitchViews()
    {
        _dockerServiceMock
            .Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ContainerInfo>());
        
        _dockerServiceMock
            .Setup(x => x.GetImagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ImageInfo>());

        var vm = new MainWindowViewModel(_dockerServiceMock.Object, _themeServiceMock.Object, _dockerClient!, _dialogServiceMock.Object);

        vm.ShowContainers.Should().BeTrue();
        vm.ShowImages.Should().BeFalse();
        vm.ContainersTextColor.Should().Be("#FFFFFF");
        vm.ImagesTextColor.Should().Be("#8888AA");

        vm.ShowImagesViewCommand.Execute(null);

        vm.ShowContainers.Should().BeFalse();
        vm.ShowImages.Should().BeTrue();
        vm.ContainersTextColor.Should().Be("#8888AA");
        vm.ImagesTextColor.Should().Be("#FFFFFF");
        
        vm.Dispose();
    }

    [Fact]
    public async Task MainWindowViewModel_Should_GetDockerVersion()
    {
        var systemInfo = new DockerSystemInfo(
            ServerVersion: "24.0.7",
            ApiVersion: "1.43",
            OS: "linux",
            Architecture: "x86_64",
            Containers: 5,
            ContainersRunning: 2,
            ContainersPaused: 0,
            ContainersStopped: 3,
            Images: 10,
            MemoryTotal: 8000000000,
            Driver: "overlay2"
        );

        _dockerServiceMock
            .Setup(x => x.GetSystemInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(systemInfo);

        var vm = new MainWindowViewModel(_dockerServiceMock.Object, _themeServiceMock.Object, _dockerClient!, _dialogServiceMock.Object);
        
        await Task.Delay(100); // Let constructor task complete

        vm.DockerVersion.Should().Be("v24.0.7");
        vm.DockerStatusColor.Should().Be("#4ECDC4");
        
        vm.Dispose();
    }
}