using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Repository for configured skip segments.
/// </summary>
public interface ISkipRepository
{
    /// <summary>
    /// Initializes the repository.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured skip segments for an item.
    /// </summary>
    Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Replaces all configured skip segments for an item.
    /// </summary>
    Task SaveSegmentsAsync(
        Guid itemId,
        IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Deletes all configured skip segments for an item.
    /// </summary>
    Task DeleteSegmentsAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an item has configured skip segments.
    /// </summary>
    Task<bool> HasSegmentsAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured media items.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetConfiguredItemsAsync(
        CancellationToken cancellationToken = default
    );
}
