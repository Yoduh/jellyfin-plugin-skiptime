using Jellyfin.Plugin.SkipTime.Data;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaSegments;

namespace Jellyfin.Plugin.SkipTime.Providers;

/// <summary>
/// Provides Skip Time segments to Jellyfin.
/// </summary>
public sealed class SkipSegmentProvider : IMediaSegmentProvider
{
    private readonly SkipTime.Interfaces.ISkipSegmentCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipSegmentProvider"/> class.
    /// </summary>
    /// <param name="cache">The skip segment cache.</param>
    public SkipSegmentProvider(SkipTime.Interfaces.ISkipSegmentCache cache)
    {
        _cache = cache;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MediaSegment>> GetMediaSegments(
        BaseItem item,
        CancellationToken cancellationToken
    )
    {
        var segments = await _cache
            .GetSegmentsAsync(item.Id, cancellationToken)
            .ConfigureAwait(false);

        return segments.Select(segment => new MediaSegment
        {
            Type = MediaSegmentType.Recap,
            StartTicks = segment.StartTicks,
            EndTicks = segment.EndTicks
        });
    }

    /// <inheritdoc />
    public bool Supports(BaseItem item)
    {
        return item is Video;
    }
}
