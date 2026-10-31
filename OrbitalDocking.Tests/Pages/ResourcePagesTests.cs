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
    private readonly Mock<IDialogService> _dialogs = new();
    private readonly AppState _state = new();

    private ImagesPageViewModel CreateImagesPage() =>
        new(_docker.Object, _dialogs.Object, _state, new ImmediateUiDispatcher());

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
    public async Task SortBy_OrdersItemsAndFlipsDirectionOnRepeat()
    {
        _docker.ReturnsImages(Fixtures.Image("redis", size: 300), Fixtures.Image("nginx", size: 100), Fixtures.Image("alpine", size: 200));
        var page = CreateImagesPage();
        await page.RefreshAsync();

        page.SortByCommand.Execute("size");
        page.Items.Select(i => i.Repository).Should().Equal("nginx", "alpine", "redis");

        page.SortByCommand.Execute("size");
        page.SortDescending.Should().BeTrue();
        page.Items.Select(i => i.Repository).Should().Equal("redis", "alpine", "nginx");
    }

    [Fact]
    public async Task RemoveSelected_AsksForConfirmationThenRemovesEachAndRefreshes()
    {
        _docker.ReturnsImages(Fixtures.Image("nginx"), Fixtures.Image("redis"), Fixtures.Image("alpine"));
        _dialogs.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
        var page = CreateImagesPage();
        await page.RefreshAsync();
        foreach (var image in page.Items.Where(i => i.Repository != "alpine"))
            page.SelectedItems.Add(image);
        page.SelectedCount.Should().Be(2);

        await page.RemoveSelectedCommand.ExecuteAsync(null);

        _docker.Verify(x => x.RemoveImageAsync("sha256:nginxlatest", true, It.IsAny<CancellationToken>()), Times.Once);
        _docker.Verify(x => x.RemoveImageAsync("sha256:redislatest", true, It.IsAny<CancellationToken>()), Times.Once);
        _docker.Verify(x => x.RemoveImageAsync("sha256:alpinelatest", true, It.IsAny<CancellationToken>()), Times.Never);
        _docker.Verify(x => x.GetImagesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _state.StatusMessage.Should().Be("Removed 2 images");
    }

    [Fact]
    public async Task RemoveSelected_DoesNothingWhenDeclined()
    {
        _docker.ReturnsImages(Fixtures.Image("nginx"));
        _dialogs.Setup(d => d.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
        var page = CreateImagesPage();
        await page.RefreshAsync();

        page.SelectedItems.Add(page.Items.Single());

        await page.RemoveSelectedCommand.ExecuteAsync(null);

        _docker.Verify(x => x.RemoveImageAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveImage_CallsDockerThenRefreshes()
    {
        _docker.ReturnsImages(Fixtures.Image("nginx"));
        var page = CreateImagesPage();
        await page.RefreshAsync();

        await page.RemoveItemCommand.ExecuteAsync(page.Items.Single());

        _docker.Verify(x => x.RemoveImageAsync("sha256:nginxlatest", true, It.IsAny<CancellationToken>()), Times.Once);
        _docker.Verify(x => x.GetImagesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _state.StatusMessage.Should().Be("Removed image nginx:latest");
    }
}
