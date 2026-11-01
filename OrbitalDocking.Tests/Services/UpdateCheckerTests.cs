using System.Net;
using FluentAssertions;
using OrbitalDocking.Services;
using Xunit;

namespace OrbitalDocking.Tests.Services;

public class UpdateCheckerTests
{
    private static GitHubUpdateChecker Checker(string currentVersion, HttpStatusCode status, string body)
    {
        var handler = new StubHandler(status, body);
        return new GitHubUpdateChecker(new HttpClient(handler), new Version(currentVersion));
    }

    private const string ReleaseJson = """{ "tag_name": "v0.4.0", "html_url": "https://github.com/Nechja/Orbital/releases/tag/v0.4.0" }""";

    [Fact]
    public async Task ReportsAnUpdate_WhenTheLatestReleaseIsNewer()
    {
        var result = await Checker("0.3.0", HttpStatusCode.OK, ReleaseJson).CheckAsync();

        result.IsUpdateAvailable.Should().BeTrue();
        result.Latest.Should().Be(new Version(0, 4, 0));
        result.ReleaseUrl.Should().EndWith("/v0.4.0");
    }

    [Theory]
    [InlineData("0.4.0")]
    [InlineData("0.5.1")]
    public async Task ReportsUpToDate_WhenRunningTheLatestOrNewer(string current)
    {
        var result = await Checker(current, HttpStatusCode.OK, ReleaseJson).CheckAsync();

        result.IsUpdateAvailable.Should().BeFalse();
        result.IsUpToDate.Should().BeTrue();
    }

    [Fact]
    public async Task DevelopmentBuilds_NeverReportAnUpdate()
    {
        var result = await Checker("0.0.0", HttpStatusCode.OK, ReleaseJson).CheckAsync();

        result.IsDevelopmentBuild.Should().BeTrue();
        result.IsUpdateAvailable.Should().BeFalse();
        result.Latest.Should().Be(new Version(0, 4, 0), "the latest version is still reported for display");
    }

    [Fact]
    public async Task ReportsAnError_InsteadOfThrowing_WhenGitHubIsUnavailable()
    {
        var result = await Checker("0.3.0", HttpStatusCode.ServiceUnavailable, "").CheckAsync();

        result.Error.Should().Contain("503");
        result.IsUpdateAvailable.Should().BeFalse();
        result.IsUpToDate.Should().BeFalse();
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3-beta.1+abc", "1.2.3")]
    [InlineData("0.3", "0.3.0")]
    public void ParsesTagsAndInformationalVersions(string text, string expected)
    {
        GitHubUpdateChecker.TryParseVersion(text, out var version).Should().BeTrue();
        version.Should().Be(new Version(expected));
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
    }
}
