using FluentAssertions;
using OrbitalDocking.Models;
using OrbitalDocking.Tests.TestSupport;
using OrbitalDocking.ViewModels;
using OrbitalDocking.ViewModels.Pages;
using Xunit;

namespace OrbitalDocking.Tests.Pages;

public class LogsPageTests
{
    private static LogsPageViewModel CreatePage(FakeContainerLogs logs, params ContainerViewModel[] containers)
    {
        var page = new LogsPageViewModel(logs, new ImmediateUiDispatcher(), new AppState());
        foreach (var c in containers) page.AvailableContainers.Add(c);
        return page;
    }

    [Fact]
    public async Task SelectingAContainer_FollowsItsLogs()
    {
        var logs = new FakeContainerLogs().With("a", "hello\n");
        var web = new ContainerViewModel(Fixtures.Container("a", "web"));
        var page = CreatePage(logs, web);

        page.SelectedContainer = web;
        await Task.Delay(50);

        page.Logs.Content.Should().Be("hello");
        page.Logs.ContainerName.Should().Be("web");
    }

    [Fact]
    public async Task ShowErrors_ListsRunningContainersWithErrorLines()
    {
        var logs = new FakeContainerLogs()
            .With("a", "ok\nERROR: disk full\nfine\n")
            .With("b", "all good\n")
            .With("c", "FATAL crash\n");
        var page = CreatePage(logs,
            new ContainerViewModel(Fixtures.Container("a", "web")),
            new ContainerViewModel(Fixtures.Container("b", "db")),
            new ContainerViewModel(Fixtures.Container("c", "worker", ContainerState.Exited)));

        await page.ShowErrorsCommand.ExecuteAsync(null);

        page.ShowingErrorOverview.Should().BeTrue();
        page.HasErrors.Should().BeTrue();
        page.ErrorContainers.Should().ContainSingle().Which.Should().BeEquivalentTo(new { ContainerName = "web", ErrorCount = 1 });
    }

    [Fact]
    public async Task SearchAll_ReportsMatchesPerRunningContainer()
    {
        var logs = new FakeContainerLogs().With("a", "GET /\nPOST /login\n").With("b", "POST /login failed\n");
        var page = CreatePage(logs,
            new ContainerViewModel(Fixtures.Container("a", "web")),
            new ContainerViewModel(Fixtures.Container("b", "api")));
        page.Logs.SearchFilter = "login";

        await page.SearchAllLogsCommand.ExecuteAsync(null);

        page.Logs.Content.Should().Contain("--- web (1 matches) ---").And.Contain("--- api (1 matches) ---").And.NotContain("\r");
        page.Logs.IsFollowing.Should().BeFalse();
    }
}
