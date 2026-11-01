using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

/// <summary>Compares the running version with the latest GitHub release of Nechja/Orbital.</summary>
public sealed class GitHubUpdateChecker : IUpdateChecker
{
    public const string LatestReleaseUrl = "https://api.github.com/repos/Nechja/Orbital/releases/latest";

    private readonly HttpClient _http;

    public GitHubUpdateChecker() : this(new HttpClient(), ReadAssemblyVersion()) { }

    public GitHubUpdateChecker(HttpClient http, Version currentVersion)
    {
        _http = http;
        CurrentVersion = currentVersion;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OrbitalDocking");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.Timeout = TimeSpan.FromSeconds(10);
    }

    public Version CurrentVersion { get; }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _http.GetAsync(LatestReleaseUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new UpdateCheckResult(CurrentVersion, null, null, $"GitHub returned {(int)response.StatusCode}");

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var root = json.RootElement;
            var tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
            var url = root.TryGetProperty("html_url", out var u) ? u.GetString() : null;

            return TryParseVersion(tag, out var latest)
                ? new UpdateCheckResult(CurrentVersion, latest, url, null)
                : new UpdateCheckResult(CurrentVersion, null, url, $"Unrecognised release tag '{tag}'");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new UpdateCheckResult(CurrentVersion, null, null, ex.Message);
        }
    }

    /// <summary>Accepts "v1.2.3", "1.2.3.4", or "1.2"; ignores any pre-release or build suffix. Always yields major.minor.patch.</summary>
    public static bool TryParseVersion(string text, out Version version)
    {
        version = new Version(0, 0, 0);
        var core = text.Trim().TrimStart('v', 'V');
        var end = core.IndexOfAny(new[] { '-', '+' });
        if (end >= 0) core = core[..end];
        if (!Version.TryParse(core.Contains('.') ? core : core + ".0", out var parsed))
            return false;

        version = new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0));
        return true;
    }

    /// <summary>The version stamped at build time (-p:Version=...), without the commit-hash suffix.</summary>
    private static Version ReadAssemblyVersion()
    {
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return informational is not null && TryParseVersion(informational, out var v) ? v : new Version(0, 0, 0);
    }
}
