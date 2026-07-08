using System;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// Represents a configured skip segment.
/// </summary>
public sealed class SkipSegment
{
    /// <summary>
    /// Database identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Media item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Start position in ticks.
    /// </summary>
    public long StartTicks { get; set; }

    /// <summary>
    /// End position in ticks.
    /// </summary>
    public long EndTicks { get; set; }
}
