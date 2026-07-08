using Jellyfin.Plugin.SkipTime.Api.Dtos;
using Jellyfin.Plugin.SkipTime.Data;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SkipTime.Api;

/// <summary>
/// Skip Time API.
/// </summary>
[ApiController]
[Authorize(Policy = "DefaultAuthorization")]
[Route("SkipTime")]
public sealed class SkipController : ControllerBase
{
    private readonly ISkipRepository _repository;
    private readonly ISkipSegmentCache _cache;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipController"/> class.
    /// </summary>
    public SkipController(
        ISkipRepository repository,
        ISkipSegmentCache cache,
        ILibraryManager libraryManager
    )
    {
        _repository = repository;
        _cache = cache;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Searches the media library.
    /// </summary>
    [HttpGet("Search")]
    public ActionResult<IEnumerable<SearchResultDto>> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(Array.Empty<SearchResultDto>());
        }

        var results = _libraryManager
            .GetItemList(
                new InternalItemsQuery
                {
                    SearchTerm = query,
                    Recursive = true,
                    IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Episode]
                }
            )
            .Select(item => new SearchResultDto
            {
                ItemId = item.Id,
                Name = item.Name,
                Type = item.GetType().Name,
                ProductionYear = item.ProductionYear
            });

        return Ok(results);
    }

    /// <summary>
    /// Gets all configured skip segments for an item.
    /// </summary>
    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SkipSegment>>> Get(
        Guid itemId,
        CancellationToken cancellationToken
    )
    {
        var segments = await _cache
            .GetSegmentsAsync(itemId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(segments);
    }

    /// <summary>
    /// Replaces all skip segments for an item.
    /// </summary>
    [HttpPost("{itemId:guid}")]
    public async Task<IActionResult> Save(
        Guid itemId,
        [FromBody] IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(segments);

        await _repository
            .SaveSegmentsAsync(itemId, segments, cancellationToken)
            .ConfigureAwait(false);

        _cache.Invalidate(itemId);

        return NoContent();
    }

    /// <summary>
    /// Deletes all skip segments for an item.
    /// </summary>
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId, CancellationToken cancellationToken)
    {
        await _repository.DeleteSegmentsAsync(itemId, cancellationToken).ConfigureAwait(false);

        _cache.Invalidate(itemId);

        return NoContent();
    }
}
