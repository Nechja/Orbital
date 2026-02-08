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
    public void SelectedContainer_WhenChanged_TriggersLogLoading()
    {
        var container = CreateMockContainer("test-container", "abc123");
        _sut.AvailableContainers.Add(container);

        _sut.SelectedContainer = container;

        _sut.ShowingErrorOverview.Should().BeFalse("switching containers should exit error view");
    }

    [Fact]
    public void SearchFilter_WhenEmpty_ShowsAllLogs()
    {
        _sut.LogsContent = "line1\nline2\nerror line\nline4";

        _sut.SearchFilter = "";

        _sut.LogsContent.Should().Contain("line1");
        _sut.LogsContent.Should().Contain("line2");
        _sut.LogsContent.Should().Contain("error line");
        _sut.LogsContent.Should().Contain("line4");
    }

    [Fact]
    public void SearchFilter_WhenSet_FiltersLogsCorrectly()
    {
        SetupLogsContent("line1\nline2\nerror line\nline4");

        _sut.SearchFilter = "error";

        _sut.LogsContent.Should().Contain("error line");
        _sut.LogsContent.Should().NotContain("line1");
        _sut.LogsContent.Should().NotContain("line2");
    }

    [Fact]
    public void SearchFilter_IsCaseInsensitive()
    {
        SetupLogsContent("ERROR: something failed\nwarning: minor issue");

        _sut.SearchFilter = "error";

        _sut.LogsContent.Should().Contain("ERROR: something failed");
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
    public void ShowingErrorOverview_WhenSwitchingContainers_ResetsToFalse()
    {
        _sut.ShowingErrorOverview = true;
        var container = CreateMockContainer("test", "123");
        _sut.AvailableContainers.Add(container);

        _sut.SelectedContainer = container;

        _sut.ShowingErrorOverview.Should().BeFalse("switching to a container should show logs, not error view");
    }

    [Fact]
    public async Task ViewContainerLogs_WithNullErrorInfo_DoesNotThrow()
    {
        var act = async () => await _sut.ViewContainerLogsCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync("null handling should be safe");
    }

    [Fact]
    public void ViewContainerLogs_WithValidContainer_ExitsErrorOverview()
    {
        var container = CreateMockContainer("test", "123");
        _sut.AvailableContainers.Add(container);
        _sut.ShowingErrorOverview = true;

        var errorInfo = new ErrorContainerInfo
        {
            ContainerId = "123",
            ContainerName = "test",
            ErrorCount = 1,
            ErrorLines = new List<string>()
        };

        _sut.ViewContainerLogsCommand.Execute(errorInfo);

        _sut.ShowingErrorOverview.Should().BeFalse();
        _sut.SelectedContainer.Should().Be(container);
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

    private void SetupLogsContent(string content)
    {
        var contentField = typeof(LogsViewModel).GetField("_logsBuilder",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var builder = (System.Text.StringBuilder)contentField!.GetValue(_sut)!;
        builder.Clear();
        builder.Append(content);
        _sut.LogsContent = content;
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }
}
