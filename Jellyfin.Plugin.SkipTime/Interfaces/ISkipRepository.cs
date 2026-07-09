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
    /// <param name="cancellationToken">A token to cancel the initialization operation.</param>
    /// <returns>A task that completes when initialization is finished.</returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured skip segments for an item.
    /// </summary>
    /// <param name="itemId">The GUID of the media item to retrieve segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SkipSegment"/> for the specified item. May be empty.</returns>
    Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all configured skip segments for an item.
    /// </summary>
    /// <param name="itemId">The GUID of the media item to save segments for.</param>
    /// <param name="segments">The collection of segments to persist. Must not be null.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the segments have been saved.</returns>
    Task SaveSegmentsAsync(
        Guid itemId,
        IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all configured skip segments for an item.
    /// </summary>
    /// <param name="itemId">The GUID of the media item to delete segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the delete operation is finished.</returns>
    Task DeleteSegmentsAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether an item has configured skip segments.
    /// </summary>
    /// <param name="itemId">The GUID of the media item to check.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the item has configured segments; otherwise false.</returns>
    Task<bool> HasSegmentsAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all configured media items.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of GUIDs for media items that have configured skip segments.</returns>
    Task<IReadOnlyList<Guid>> GetConfiguredItemsAsync(
        CancellationToken cancellationToken = default);
}
