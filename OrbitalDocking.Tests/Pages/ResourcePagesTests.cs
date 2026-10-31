using FluentAssertions;
using Moq;
using OrbitalDocking.Services;
using OrbitalDocking.Tests.TestSupport;
using OrbitalDocking.ViewModels;
using OrbitalDocking.ViewModels.Pages;
using Xunit;

namespace OrbitalDocking.Tests.Pages;

/// <summary>Images stands in for the three list pages, which share ResourcePageViewModel.</summary>
public class ResourcePagesTests
{
    private readonly Mock<IDockerService> _docker = Fixtures.DockerService();
    private readonly AppState _state = new();

    private ImagesPageViewModel CreateImagesPage() =>
        new(_docker.Object, Mock.Of<IDialogService>(), _state, new ImmediateUiDispatcher());

    [Fact]
    public async Task Refresh_LoadsSortedItemsAndCount()
    {
        _docker.ReturnsImages(Fixtures.Image("redis", "7"), Fixtures.Image("nginx"), Fixtures.Image("nginx", "alpine"));
        var page = CreateImagesPage();

        await page.RefreshAsync();

        page.Count.Should().Be(3);
        page.Items.Select(i => $"{i.Repository}:{i.Tag}").Should().Equal("nginx:alpine", "nginx:latest", "redis:7");
    }

    [Fact]
    public async Task Search_FiltersItemsWithoutReloading()
    {
        _docker.ReturnsImages(Fixtures.Image("redis"), Fixtures.Image("nginx"));
        var page = CreateImagesPage();
        await page.RefreshAsync();

        page.SearchText = "NGINX";

        page.Items.Should().ContainSingle().Which.Repository.Should().Be("nginx");
        page.Count.Should().Be(2, "the sidebar badge shows the total, not the filtered count");
        _docker.Verify(x => x.GetImagesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveImage_CallsDockerThenRefreshes()
    {
        _docker.ReturnsImages(Fixtures.Image("nginx"));
        var page = CreateImagesPage();
        await page.RefreshAsync();

        await page.RemoveImageCommand.ExecuteAsync(page.Items.Single());

        _docker.Verify(x => x.RemoveImageAsync("sha256:nginxlatest", true, It.IsAny<CancellationToken>()), Times.Once);
        _docker.Verify(x => x.GetImagesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _state.StatusMessage.Should().Be("Removed nginx:latest");
    }
}
