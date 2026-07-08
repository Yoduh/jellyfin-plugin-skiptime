using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Provides cached skip segments.
/// </summary>
public interface ISkipSegmentCache
{
    /// <summary>
    /// Gets the configured skip segments for an item.
    /// </summary>
    Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Invalidates a cached media item.
    /// </summary>
    void Invalidate(Guid itemId);

    /// <summary>
    /// Clears the cache.
    /// </summary>
    void Clear();
}
