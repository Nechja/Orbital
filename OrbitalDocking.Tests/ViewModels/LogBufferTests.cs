using FluentAssertions;
using OrbitalDocking.Tests.TestSupport;
using OrbitalDocking.ViewModels;
using Xunit;

namespace OrbitalDocking.Tests.ViewModels;

public class LogBufferTests
{
    private static async Task Settle() => await Task.Delay(50);

    [Fact]
    public async Task Follow_AppendsChunksAndJoinsPartialLines()
    {
        var logs = new FakeContainerLogs().With("a", "first li", "ne\nsecond line\nthi", "rd");
        var buffer = new LogBufferViewModel(logs, new ImmediateUiDispatcher());

        buffer.Follow("a", "web");
        await Settle();

        buffer.Content.Should().Be("first line\nsecond line\nthird");
        buffer.ContainerName.Should().Be("web");
    }

    [Fact]
    public async Task SearchFilter_ShowsOnlyMatchingLinesWithoutLosingTheBuffer()
    {
        var logs = new FakeContainerLogs().With("a", "GET /\nERROR boom\nGET /health\n");
        var buffer = new LogBufferViewModel(logs, new ImmediateUiDispatcher());
        buffer.Follow("a", "web");
        await Settle();

        buffer.SearchFilter = "error";
        buffer.Content.Should().Be("ERROR boom");

        buffer.SearchFilter = "";
        buffer.Content.Should().Be("GET /\nERROR boom\nGET /health");
    }

    [Fact]
    public async Task Follow_KeepsOnlyTheMostRecentLines()
    {
        var lines = string.Concat(Enumerable.Range(1, 1200).Select(i => $"line {i}\n"));
        var logs = new FakeContainerLogs().With("a", lines);
        var buffer = new LogBufferViewModel(logs, new ImmediateUiDispatcher());

        buffer.Follow("a", "web");
        await Settle();

        var shown = buffer.Content.Split('\n');
        shown.Should().HaveCount(1000);
        shown[0].Should().Be("line 201");
        shown[^1].Should().Be("line 1200");
    }

    [Fact]
    public async Task Follow_ReplacesPreviousContainerOutput()
    {
        var logs = new FakeContainerLogs().With("a", "from a\n").With("b", "from b\n");
        var buffer = new LogBufferViewModel(logs, new ImmediateUiDispatcher());

        buffer.Follow("a", "web");
        await Settle();
        buffer.Follow("b", "db");
        await Settle();

        buffer.Content.Should().Be("from b");
    }
}
