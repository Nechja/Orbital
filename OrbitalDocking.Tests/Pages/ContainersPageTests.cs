using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrbitalDocking.Models;
using OrbitalDocking.Services;
using OrbitalDocking.Tests.TestSupport;
using OrbitalDocking.ViewModels;
using OrbitalDocking.ViewModels.Pages;
using Xunit;

namespace OrbitalDocking.Tests.Pages;

public class ContainersPageTests
{
    private readonly Mock<IDockerService> _docker = Fixtures.DockerService();
    private readonly AppState _state = new();

    private ContainersPageViewModel CreatePage() =>
        new(_docker.Object, null, new FakeContainerLogs(), Mock.Of<IDialogService>(), _state, new ImmediateUiDispatcher(), NullLogger<ContainersPageViewModel>.Instance);

    [Fact]
    public async Task Refresh_GroupsContainersIntoStacksAndStandalone()
    {
        _docker.ReturnsContainers(
            Fixtures.Container("a", "web", stack: "shop"),
            Fixtures.Container("b", "db", ContainerState.Exited, stack: "shop"),
            Fixtures.Container("c", "redis"));
        var page = CreatePage();

        await page.RefreshAsync();

        page.Count.Should().Be(3);
        page.RunningCount.Should().Be(2);
        page.StoppedCount.Should().Be(1);
        page.Stacks.Should().ContainSingle().Which.Containers.Select(c => c.Name).Should().Equal("db", "web");
        page.StandaloneContainers.Select(c => c.Name).Should().Equal("redis");
    }

    [Fact]
    public async Task Refresh_ReusesViewModelsAndDropsRemovedContainers()
    {
        _docker.ReturnsContainers(Fixtures.Container("a", "web"), Fixtures.Container("b", "db"));
        var page = CreatePage();
        await page.RefreshAsync();
        var web = page.Containers.Single(c => c.Id == "a");

        _docker.ReturnsContainers(Fixtures.Container("a", "web", ContainerState.Exited));
        await page.RefreshAsync();

        page.Containers.Should().ContainSingle().Which.Should().BeSameAs(web);
        web.IsStopped.Should().BeTrue();
    }

    [Fact]
    public async Task Search_FiltersByNameImageOrStack()
    {
        _docker.ReturnsContainers(
            Fixtures.Container("a", "web", stack: "shop"),
            Fixtures.Container("b", "redis", image: "redis:7"),
            Fixtures.Container("c", "worker"));
        var page = CreatePage();
        await page.RefreshAsync();

        page.SearchText = "shop";
        page.Stacks.Should().ContainSingle();
        page.StandaloneContainers.Should().BeEmpty();

        page.SearchText = "redis";
        page.Stacks.Should().BeEmpty();
        page.StandaloneContainers.Select(c => c.Name).Should().Equal("redis");

        page.SearchText = "";
        page.StandaloneContainers.Should().HaveCount(2);
    }

    [Fact]
    public async Task StartContainer_CallsDockerAndReportsStatus()
    {
        _docker.ReturnsContainers(Fixtures.Container("a", "web", ContainerState.Exited));
        var page = CreatePage();
        await page.RefreshAsync();

        await page.StartContainerCommand.ExecuteAsync(page.Containers.Single());

        _docker.Verify(x => x.StartContainerAsync("a", It.IsAny<CancellationToken>()), Times.Once);
        _state.StatusMessage.Should().Be("Started web");
    }

    [Fact]
    public async Task Refresh_ReportsErrorsInStatusBar()
    {
        _docker.Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOr.Error.Failure(description: "daemon unreachable"));
        var page = CreatePage();

        await page.RefreshAsync();

        _state.StatusMessage.Should().Contain("daemon unreachable");
        page.Containers.Should().BeEmpty();
    }

    [Fact]
    public async Task Select_TogglesSelectionAndClearsItWhenTheContainerDisappears()
    {
        _docker.ReturnsContainers(Fixtures.Container("a", "web"), Fixtures.Container("b", "db"));
        var page = CreatePage();
        await page.RefreshAsync();
        var web = page.Containers.Single(c => c.Id == "a");

        page.SelectCommand.Execute(web);
        page.SelectedContainer.Should().BeSameAs(web);
        web.IsSelected.Should().BeTrue();

        page.SelectCommand.Execute(web);
        page.SelectedContainer.Should().BeNull("selecting the selected row deselects it");
        web.IsSelected.Should().BeFalse();

        page.SelectCommand.Execute(web);
        _docker.ReturnsContainers(Fixtures.Container("b", "db"));
        await page.RefreshAsync();
        page.SelectedContainer.Should().BeNull("the selected container was removed");
    }
}
