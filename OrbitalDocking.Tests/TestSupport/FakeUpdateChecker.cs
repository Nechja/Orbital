using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

public sealed class FakeUpdateChecker : IUpdateChecker
{
    public Version CurrentVersion { get; init; } = new(0, 3, 0);
    public Version? Latest { get; init; }
    public string? Error { get; init; }
    public int Calls { get; private set; }

    public Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(new UpdateCheckResult(CurrentVersion, Latest, Latest is null ? null : $"https://example.test/v{Latest}", Error));
    }
}
