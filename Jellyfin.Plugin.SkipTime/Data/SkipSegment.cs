using System;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Represents a configured skip segment.
/// </summary>
public sealed class SkipSegment
{
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the media item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the start position in ticks.
    /// </summary>
    public long StartTicks { get; set; }

    /// <summary>
    /// Gets or sets the end position in ticks.
    /// </summary>
    public long EndTicks { get; set; }
}
