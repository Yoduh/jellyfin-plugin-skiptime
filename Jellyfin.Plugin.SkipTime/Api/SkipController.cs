using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SkipTime.Api.Dtos;
using Jellyfin.Plugin.SkipTime.Data;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.SkipTime.Api;

/// <summary>
/// Skip Time API.
/// </summary>
[Authorize]
[ApiController]
[Route("SkipTime")]
public sealed class SkipController : ControllerBase
{
    private readonly ISkipRepository _repository;
    private readonly ISkipSegmentCache _cache;
    private readonly ILibraryManager _libraryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipController"/> class.
    /// </summary>
    /// <param name="repository">Repository used to persist and retrieve skip segments.</param>
    /// <param name="cache">Cache used to store and retrieve skip segments for quick access.</param>
    /// <param name="libraryManager">Library manager used to query the media library.</param>
    public SkipController(
        ISkipRepository repository,
        ISkipSegmentCache cache,
        ILibraryManager libraryManager)
    {
        _repository = repository;
        _cache = cache;
        _libraryManager = libraryManager;
    }

    /// <summary>
    /// Searches the media library.
    /// </summary>
    /// <param name="query">The search term to match against media items. If null or whitespace an empty result set is returned.</param>
    /// <returns>
    /// 200 OK with a collection of SearchResultDto matching the query.
    /// If the query is null or whitespace an empty array is returned. (Endpoint requires authorization and may also return 401/403.)
    /// </returns>
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
                }).Select(item => new SearchResultDto
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
    /// <param name="itemId">The GUID of the media item to retrieve skip segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>200 OK with a read-only list of <see cref="SkipSegment"/> for the specified item. Returns an empty list if no segments are configured. May return 401/403 when the caller is not authorized.</returns>
    [HttpGet("{itemId:guid}")]
    public async Task<ActionResult<IReadOnlyList<SkipSegment>>> Get(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var segments = await _cache
            .GetSegmentsAsync(itemId, cancellationToken)
            .ConfigureAwait(false);

        return Ok(segments);
    }

    /// <summary>
    /// Replaces all skip segments for an item.
    /// </summary>
    /// <param name="itemId">The GUID of the media item to save skip segments for.</param>
    /// <param name="segments">The collection of <see cref="SkipSegment"/> to save. This replaces any existing segments for the item.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content on success. Throws <see cref="ArgumentNullException"/> if <paramref name="segments"/> is null. May return 401/403 when the caller is not authorized.</returns>
    [HttpPost("{itemId:guid}")]
    public async Task<IActionResult> Save(
        Guid itemId,
        [FromBody] IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken)
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
    /// <param name="itemId">The GUID of the media item to delete skip segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>204 No Content on success. May return 401/403 when the caller is not authorized.</returns>
    [HttpDelete("{itemId:guid}")]
    public async Task<IActionResult> Delete(Guid itemId, CancellationToken cancellationToken)
    {
        await _repository.DeleteSegmentsAsync(itemId, cancellationToken).ConfigureAwait(false);

        _cache.Invalidate(itemId);

        return NoContent();
    }
}
