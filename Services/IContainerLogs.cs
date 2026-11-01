using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

/// <summary>Reads container log output. Abstracted so log view models can be tested without Docker.</summary>
public interface IContainerLogs
{
    /// <summary>Yields raw log chunks as they arrive, starting with the last <paramref name="tail"/> lines, until cancelled.</summary>
    IAsyncEnumerable<string> FollowAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken);

    /// <summary>Returns the last <paramref name="tail"/> lines without following.</summary>
    Task<string> ReadTailAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken);
}
