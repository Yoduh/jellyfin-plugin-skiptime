using System;

namespace Jellyfin.Plugin.SkipTime.Api.Dtos;

/// <summary>
/// Search result returned by the admin UI.
/// </summary>
public sealed class SearchResultDto
{
    public Guid ItemId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public int? ProductionYear { get; set; }
}
