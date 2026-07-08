using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Runtime cache for skip segments.
/// </summary>
public sealed class SkipSegmentCache : SkipTime.Interfaces.ISkipSegmentCache
{
    private readonly SkipTime.Interfaces.ISkipRepository _repository;

    private readonly ConcurrentDictionary<Guid, IReadOnlyList<SkipSegment>> _cache = new();

    public SkipSegmentCache(SkipTime.Interfaces.ISkipRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    )
    {
        if (_cache.TryGetValue(itemId, out var segments))
        {
            return segments;
        }

        segments = await _repository
            .GetSegmentsAsync(itemId, cancellationToken)
            .ConfigureAwait(false);

        _cache[itemId] = segments;

        return segments;
    }

    /// <inheritdoc/>
    public void Invalidate(Guid itemId)
    {
        _cache.TryRemove(itemId, out _);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _cache.Clear();
    }
}
