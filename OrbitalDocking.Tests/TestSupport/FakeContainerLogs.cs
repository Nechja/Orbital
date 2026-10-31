using System.Runtime.CompilerServices;
using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

/// <summary>Serves canned log chunks per container id; FollowAsync completes after the last chunk.</summary>
public sealed class FakeContainerLogs : IContainerLogs
{
    private readonly Dictionary<string, string[]> _chunks = new();

    public FakeContainerLogs With(string containerId, params string[] chunks)
    {
        _chunks[containerId] = chunks;
        return this;
    }

    public async IAsyncEnumerable<string> FollowAsync(string containerId, bool timestamps, int tail, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var chunk in _chunks.GetValueOrDefault(containerId, Array.Empty<string>()))
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    public Task<string> ReadTailAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken) =>
        Task.FromResult(string.Concat(_chunks.GetValueOrDefault(containerId, Array.Empty<string>())));
}
