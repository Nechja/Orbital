using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace OrbitalDocking.Services;

public sealed class DockerContainerLogs : IContainerLogs
{
    private readonly DockerClient _client;

    public DockerContainerLogs(DockerClient client)
    {
        _client = client;
    }

    public async IAsyncEnumerable<string> FollowAsync(string containerId, bool timestamps, int tail, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await _client.Containers.GetContainerLogsAsync(containerId, false, Parameters(timestamps, tail, follow: true), cancellationToken);
        var buffer = new byte[4096];
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
            if (result.Count > 0)
                yield return Encoding.UTF8.GetString(buffer, 0, result.Count);
            else if (result.EOF)
                yield break;
        }
    }

    public async Task<string> ReadTailAsync(string containerId, bool timestamps, int tail, CancellationToken cancellationToken)
    {
        var text = new StringBuilder();
        await foreach (var chunk in ReadAsync(containerId, timestamps, tail, cancellationToken))
            text.Append(chunk);
        return text.ToString();
    }

    private async IAsyncEnumerable<string> ReadAsync(string containerId, bool timestamps, int tail, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var stream = await _client.Containers.GetContainerLogsAsync(containerId, false, Parameters(timestamps, tail, follow: false), cancellationToken);
        var buffer = new byte[4096];
        while (true)
        {
            var result = await stream.ReadOutputAsync(buffer, 0, buffer.Length, cancellationToken);
            if (result.Count > 0)
                yield return Encoding.UTF8.GetString(buffer, 0, result.Count);
            else if (result.EOF)
                yield break;
        }
    }

    private static ContainerLogsParameters Parameters(bool timestamps, int tail, bool follow) => new()
    {
        ShowStdout = true,
        ShowStderr = true,
        Follow = follow,
        Timestamps = timestamps,
        Tail = tail.ToString(),
    };
}
