using System.Threading.Channels;

using Haven.Application.Common.Interfaces;

namespace Haven.Infrastructure.Backup;

/// <summary>
/// Single-slot signal channel: any number of <see cref="RequestSync"/> calls between two drains by
/// <see cref="ManifestSyncBackgroundService"/> collapse into one pending signal, which is what lets
/// the background service debounce bursts of mutations into a single full manifest resync.
/// </summary>
public sealed class ManifestSyncTrigger : IManifestSyncTrigger
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    {
        FullMode = BoundedChannelFullMode.DropWrite
    });

    public ChannelReader<bool> Reader => _channel.Reader;

    public void RequestSync() => _channel.Writer.TryWrite(true);
}