using System;

namespace Jellyfin.Plugin.SkipTime.Configuration;

/// <summary>
/// Provides filesystem paths used by the Skip Time plugin.
/// </summary>
public sealed class SkipTimePaths
{
    /// <summary>
    /// Gets or sets the SQLite database path.
    /// </summary>
    public string DatabasePath { get; init; } = string.Empty;
}
