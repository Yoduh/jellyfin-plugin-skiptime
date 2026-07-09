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
    /// <param name="itemId">The GUID of the media item to retrieve segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SkipSegment"/> for the specified item.</returns>
    Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates a cached media item.
    /// </summary>
    /// <param name="itemId">The GUID of the media item whose cache entry should be invalidated.</param>
    void Invalidate(Guid itemId);

    /// <summary>
    /// Clears the cache.
    /// </summary>
    void Clear();
}
