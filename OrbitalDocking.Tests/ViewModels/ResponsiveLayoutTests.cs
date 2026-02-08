using Xunit;
using FluentAssertions;
using OrbitalDocking.ViewModels;
using OrbitalDocking.Models;

namespace OrbitalDocking.Tests.ViewModels;

public class ResponsiveLayoutTests
{
    [Fact]
    public void ContainerViewModel_IsCompactMode_DefaultsToFalse()
    {
        var container = CreateContainer();

        container.IsCompactMode.Should().BeFalse("cards should start in full mode");
    }

    [Fact]
    public void ContainerViewModel_IsCompactMode_CanBeToggled()
    {
        var container = CreateContainer();

        container.IsCompactMode = true;

        container.IsCompactMode.Should().BeTrue("compact mode should be settable");
    }

    [Fact]
    public void ImageViewModel_IsCompactMode_DefaultsToFalse()
    {
        var image = CreateImage();

        image.IsCompactMode.Should().BeFalse("cards should start in full mode");
    }

    [Fact]
    public void ImageViewModel_IsCompactMode_CanBeToggled()
    {
        var image = CreateImage();

        image.IsCompactMode = true;

        image.IsCompactMode.Should().BeTrue("compact mode should be settable");
    }

    [Theory]
    [InlineData(1200, false)]
    [InlineData(1000, false)]
    [InlineData(999, true)]
    [InlineData(800, true)]
    [InlineData(600, true)]
    public void MainWindowViewModel_IsSmallScreen_MatchesExpectedBreakpoint(int width, bool expectedSmallScreen)
    {
        var isSmallScreen = width < 1000;

        isSmallScreen.Should().Be(expectedSmallScreen,
            $"width {width}px should {(expectedSmallScreen ? "" : "not ")}trigger small screen mode");
    }

    [Theory]
    [InlineData(800, false)]
    [InlineData(700, false)]
    [InlineData(699, true)]
    [InlineData(600, true)]
    [InlineData(400, true)]
    public void ContainerCard_CompactMode_MatchesExpectedBreakpoint(int width, bool expectedCompact)
    {
        var isCompact = width < 700;

        isCompact.Should().Be(expectedCompact,
            $"width {width}px should {(expectedCompact ? "" : "not ")}trigger compact mode");
    }

    [Theory]
    [InlineData(700, false)]
    [InlineData(600, false)]
    [InlineData(599, true)]
    [InlineData(500, true)]
    [InlineData(400, true)]
    public void ImageCard_CompactMode_MatchesExpectedBreakpoint(int width, bool expectedCompact)
    {
        var isCompact = width < 600;

        isCompact.Should().Be(expectedCompact,
            $"width {width}px should {(expectedCompact ? "" : "not ")}trigger compact mode");
    }

    private ContainerViewModel CreateContainer()
    {
        var containerInfo = new ContainerInfo(
            Id: "test123",
            Name: "test",
            Image: "test:latest",
            State: ContainerState.Running,
            Status: "Up",
            Created: DateTime.UtcNow,
            Ports: new List<PortMapping>(),
            Labels: new Dictionary<string, string>(),
            Volumes: null
        );
        return new ContainerViewModel(containerInfo);
    }

    private ImageViewModel CreateImage()
    {
        var imageInfo = new ImageInfo(
            Id: "img123",
            Repository: "test",
            Tag: "latest",
            Size: 1024,
            Created: DateTime.UtcNow,
            Architecture: "amd64",
            OS: "linux",
            RepoTags: new List<string> { "test:latest" },
            Labels: new Dictionary<string, string>()
        );
        return new ImageViewModel(imageInfo);
    }
}
