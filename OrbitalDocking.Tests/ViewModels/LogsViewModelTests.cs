using Xunit;
using FluentAssertions;
using Moq;
using Docker.DotNet;
using OrbitalDocking.ViewModels;
using System.Collections.ObjectModel;

namespace OrbitalDocking.Tests.ViewModels;

public class LogsViewModelTests : IDisposable
{
    private readonly LogsViewModel _sut;

    public LogsViewModelTests()
    {
        _sut = new LogsViewModel(null!);
    }

    [Fact]
    public void SelectedContainer_PropertyExists()
    {
        var container = CreateMockContainer("test-container", "abc123");
        _sut.AvailableContainers.Add(container);

        _sut.SelectedContainer = container;

        _sut.SelectedContainer.Should().Be(container, "selected container should be settable");
    }

    [Fact]
    public void SearchFilter_PropertyExists()
    {
        _sut.SearchFilter = "test";
        _sut.SearchFilter.Should().Be("test", "search filter property should be settable");
    }

    [Fact]
    public void LogsContent_PropertyExists()
    {
        _sut.LogsContent = "test content";
        _sut.LogsContent.Should().Be("test content", "logs content should be settable");
    }

    [Fact]
    public void HasErrors_WhenNoErrors_ReturnsFalse()
    {
        _sut.ErrorContainers.Clear();

        _sut.HasErrors.Should().BeFalse();
    }

    [Fact]
    public void HasErrors_WhenErrorsExist_ReturnsTrue()
    {
        _sut.ErrorContainers.Add(new ErrorContainerInfo
        {
            ContainerId = "abc123",
            ContainerName = "test",
            ErrorCount = 5,
            ErrorLines = new List<string> { "error line" }
        });

        _sut.HasErrors.Should().BeTrue();
    }

    [Fact]
    public void ShowingErrorOverview_PropertyExists()
    {
        _sut.ShowingErrorOverview = true;
        _sut.ShowingErrorOverview.Should().BeTrue("error overview should be settable");

        _sut.ShowingErrorOverview = false;
        _sut.ShowingErrorOverview.Should().BeFalse("error overview should be toggleable");
    }

    [Fact]
    public void ViewContainerLogsCommand_Exists()
    {
        _sut.ViewContainerLogsCommand.Should().NotBeNull("command should be initialized");
    }

    [Fact]
    public void ClearLogs_EmptiesContent()
    {
        _sut.LogsContent = "some log content";

        _sut.ClearLogsCommand.Execute(null);

        _sut.LogsContent.Should().BeEmpty();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var act = () => _sut.Dispose();

        act.Should().NotThrow("dispose should be safe to call");
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        _sut.Dispose();
        var act = () => _sut.Dispose();

        act.Should().NotThrow("dispose should be idempotent");
    }

    private ContainerViewModel CreateMockContainer(string name, string id)
    {
        var containerInfo = new Models.ContainerInfo(
            Id: id,
            Name: name,
            Image: "test:latest",
            State: Models.ContainerState.Running,
            Status: "Up",
            Created: DateTime.UtcNow,
            Ports: new List<Models.PortMapping>(),
            Labels: new Dictionary<string, string>(),
            Volumes: null
        );
        return new ContainerViewModel(containerInfo);
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }
}
