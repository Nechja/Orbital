using System;
using System.Threading;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

public sealed record UpdateCheckResult(
    Version Current,
    Version? Latest,
    string? ReleaseUrl,
    string? Error)
{
    public bool IsDevelopmentBuild => Current.Major == 0 && Current.Minor == 0 && Current.Build <= 0;
    public bool IsUpdateAvailable => Latest is not null && !IsDevelopmentBuild && Latest > Current;
    public bool IsUpToDate => Latest is not null && !IsUpdateAvailable && Error is null;
}

public interface IUpdateChecker
{
    Version CurrentVersion { get; }
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
