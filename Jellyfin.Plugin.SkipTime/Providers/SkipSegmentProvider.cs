using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.SkipTime.Data;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaSegments;
using MediaBrowser.Model;
using MediaBrowser.Model.MediaSegments;

namespace Jellyfin.Plugin.SkipTime.Providers;

/// <summary>
/// Provides configured skip segments to Jellyfin.
/// </summary>
public sealed class SkipSegmentProvider : IMediaSegmentProvider
{
    private readonly ISkipSegmentCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipSegmentProvider"/> class.
    /// </summary>
    /// <param name="cache">Cache used to store and retrieve skip segments for quick access.</param>
    public SkipSegmentProvider(
        ISkipSegmentCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public string Name => "Skip Time";

    /// <inheritdoc />
    public ValueTask<bool> Supports(BaseItem item)
    {
        return ValueTask.FromResult(item is Video);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MediaSegmentDto>> GetMediaSegments(
        MediaSegmentGenerationRequest request,
        CancellationToken cancellationToken)
    {
        var segments = new List<MediaSegmentDto>();
        var itemSegments = await _cache
            .GetSegmentsAsync(request.ItemId, cancellationToken)
            .ConfigureAwait(false);

        return segments.Select(segment => new MediaSegmentDto
        {
            ItemId = request.ItemId,
            Type = MediaSegmentType.Recap,
            StartTicks = segment.StartTicks,
            EndTicks = segment.EndTicks
        }).ToList();
    }
}
