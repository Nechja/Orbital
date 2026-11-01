using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

public interface IContainerLogs
{
    IAsyncEnumerable<string> FollowAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken);

    Task<string> ReadTailAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken);
}
