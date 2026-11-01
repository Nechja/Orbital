using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrbitalDocking.Models;
using OrbitalDocking.Services;
using OrbitalDocking.Tests.TestSupport;
using OrbitalDocking.ViewModels;
using OrbitalDocking.ViewModels.Pages;
using Xunit;

namespace OrbitalDocking.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IDockerService> _docker = Fixtures.DockerService();
    private readonly AppState _state = new();
    private readonly InMemoryPreferencesStore _preferences = new();
    private readonly FakeUpdateChecker _updates = new() { Latest = new Version(0, 4, 0) };

    private MainWindowViewModel CreateShell()
    {
        var ui = new ImmediateUiDispatcher();
        var dialogs = Mock.Of<IDialogService>();
        var logs = new FakeContainerLogs();
        return new MainWindowViewModel(
            _docker.Object,
            _preferences,
            _updates,
            _state,
            new ContainersPageViewModel(_docker.Object, null, logs, dialogs, _state, ui, NullLogger<ContainersPageViewModel>.Instance),
            new ImagesPageViewModel(_docker.Object, dialogs, _state, ui),
            new VolumesPageViewModel(_docker.Object, dialogs, _state, ui),
            new NetworksPageViewModel(_docker.Object, dialogs, _state, ui),
            new LogsPageViewModel(logs, ui, _state),
            new SettingsPageViewModel(Mock.Of<IThemeService>(), _docker.Object, _updates, _preferences, _state, NullLogger<SettingsPageViewModel>.Instance));
    }

    [Fact]
    public async Task Startup_ReportsDockerConnection()
    {
        _docker.Setup(x => x.GetSystemInfoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(ErrorOr.ErrorOrFactory.From(Fixtures.SystemInfo("24.0.7")));
        using var shell = CreateShell();

        await Task.Delay(50);

        _state.DockerVersion.Should().Be("v24.0.7");
        _state.IsDockerConnected.Should().BeTrue();
        shell.CurrentPage.Should().BeSameAs(shell.Containers);
    }

    [Fact]
    public void Navigation_SwitchesCurrentPageAndSidebarSelection()
    {
        using var shell = CreateShell();

        shell.SelectedNavPage = shell.Images;
        shell.CurrentPage.Should().BeSameAs(shell.Images);

        shell.ShowSettingsCommand.Execute(null);
        shell.CurrentPage.Should().BeSameAs(shell.Settings);
        shell.IsSettingsActive.Should().BeTrue();
        shell.SelectedNavPage.Should().BeNull("settings is not a sidebar item");

        shell.SelectedNavPage = shell.Volumes;
        shell.CurrentPage.Should().BeSameAs(shell.Volumes);
        shell.IsSettingsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ShowingLogs_ForAContainer_NavigatesWithThatContainerSelected()
    {
        _docker.ReturnsContainers(Fixtures.Container("a", "web"));
        using var shell = CreateShell();
        await shell.Containers.RefreshAsync();
        var web = shell.Containers.Containers.Single();

        shell.Containers.ShowLogsCommand.Execute(web);

        shell.CurrentPage.Should().BeSameAs(shell.Logs);
        shell.Logs.AvailableContainers.Should().Contain(web);
        shell.Logs.SelectedContainer.Should().BeSameAs(web);
    }

    [Fact]
    public async Task Startup_ChecksForUpdates_OnlyWhenEnabled()
    {
        using (var shell = CreateShell())
        {
            await Task.Delay(50);
            _updates.Calls.Should().Be(1);
            _state.IsUpdateAvailable.Should().BeTrue();
            _state.UpdateLabel.Should().Be("v0.4.0 available");
        }

        _preferences.Save(new Preferences(CheckForUpdatesOnStartup: false));
        _state.LatestUpdate = null;
        using (CreateShell())
        {
            await Task.Delay(50);
            _updates.Calls.Should().Be(1, "the startup check is off");
        }
    }

    [Fact]
    public void SidebarState_IsRestoredAndRemembered()
    {
        _preferences.Save(new Preferences(SidebarCollapsed: true));
        using var shell = CreateShell();
        shell.IsSidebarCollapsed.Should().BeTrue("the saved preference is applied on startup");

        shell.ToggleSidebarCommand.Execute(null);

        shell.IsSidebarCollapsed.Should().BeFalse();
        _preferences.Current.SidebarCollapsed.Should().BeFalse("the change is written back");
    }

    [Fact]
    public async Task Dispose_WhileARefreshIsInFlight_DoesNotThrow()
    {
        _docker.Setup(x => x.GetContainersAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken _) =>
            {
                await Task.Delay(200);
                return ErrorOr.ErrorOrFactory.From<IEnumerable<ContainerInfo>>(new List<ContainerInfo>());
            });
        var shell = CreateShell();
        await Task.Delay(50);

        var act = () => shell.Dispose();

        act.Should().NotThrow();
        await Task.Delay(300);
    }
}
