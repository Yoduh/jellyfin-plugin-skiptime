using System;

namespace Jellyfin.Plugin.SkipTime.Api.Dtos;

/// <summary>
/// Search result returned by the admin UI.
/// </summary>
public sealed class SearchResultDto
{
    /// <summary>
    /// Gets or sets the media item identifier.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the media item name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media item type (e.g., Movie, Episode).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the production year, if available.
    /// </summary>
    public int? ProductionYear { get; set; }
}
