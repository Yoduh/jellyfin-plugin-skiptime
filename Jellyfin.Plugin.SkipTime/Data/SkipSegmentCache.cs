using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Runtime cache for skip segments.
/// </summary>
public sealed class SkipSegmentCache : ISkipSegmentCache
{
    private readonly ISkipRepository _repository;

    private readonly ConcurrentDictionary<Guid, IReadOnlyList<SkipSegment>> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipSegmentCache"/> class.
    /// </summary>
    /// <param name="repository">The repository used to load segments when not present in cache.</param>
    public SkipSegmentCache(ISkipRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
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
