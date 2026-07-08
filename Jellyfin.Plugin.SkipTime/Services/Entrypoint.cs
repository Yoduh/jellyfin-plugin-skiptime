using System.Collections.Concurrent;
using Jellyfin.Plugin.SkipTime.Data;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SkipTime.Services;

/// <summary>
/// Monitors playback and automatically skips configured segments.
/// </summary>
public sealed class Entrypoint : IHostedService
{
    private readonly ISessionManager _sessionManager;
    private readonly ILibraryManager _libraryManager;
    private readonly SkipTime.Interfaces.ISkipRepository _repository;
    private readonly SkipTime.Interfaces.ISkipSegmentCache _cache;
    private readonly ILogger<Entrypoint> _logger;

    // SessionId -> EndTicks of the last segment skipped
    private readonly ConcurrentDictionary<string, long> _lastSkip = new();

    public Entrypoint(
        ISessionManager sessionManager,
        ILibraryManager libraryManager,
        SkipTime.Interfaces.ISkipRepository repository,
        SkipTime.Interfaces.ISkipSegmentCache cache,
        ILogger<Entrypoint> logger
    )
    {
        _sessionManager = sessionManager;
        _libraryManager = libraryManager;
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Skip Time.");

        await _repository.InitializeAsync(cancellationToken).ConfigureAwait(false);

        _sessionManager.PlaybackProgress += SessionManager_PlaybackProgress;
        _sessionManager.PlaybackStopped += SessionManager_PlaybackStopped;

        _logger.LogInformation("Skip Time initialized.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _sessionManager.PlaybackProgress -= SessionManager_PlaybackProgress;
        _sessionManager.PlaybackStopped -= SessionManager_PlaybackStopped;

        _cache.Clear();
        _lastSkip.Clear();

        return Task.CompletedTask;
    }

    private async void SessionManager_PlaybackProgress(object? sender, PlaybackProgressEventArgs e)
    {
        try
        {
            if (e.Item == null || e.Session == null || e.PlaybackPositionTicks == null)
            {
                return;
            }

            var itemId = e.Item.Id;

            var segments = await _cache
                .GetSegmentsAsync(itemId, CancellationToken.None)
                .ConfigureAwait(false);

            if (segments.Count == 0)
            {
                return;
            }

            var position = e.PlaybackPositionTicks.Value;

            foreach (var segment in segments)
            {
                if (position < segment.StartTicks || position >= segment.EndTicks)
                {
                    continue;
                }

                var sessionId = e.Session.Id;

                if (
                    _lastSkip.TryGetValue(sessionId, out var lastEnd)
                    && lastEnd == segment.EndTicks
                )
                {
                    return;
                }

                _logger.LogDebug(
                    "Skipping '{Item}' from {Start} to {End}.",
                    e.Item.Name,
                    segment.StartTicks,
                    segment.EndTicks
                );

                await _sessionManager
                    .SendPlaystateCommand(
                        sessionId,
                        sessionId,
                        new PlaystateRequest
                        {
                            Command = PlaystateCommand.Seek,
                            ControllingUserId = e.Session.UserId.ToString(),
                            SeekPositionTicks = segment.EndTicks
                        },
                        CancellationToken.None
                    )
                    .ConfigureAwait(false);

                _lastSkip[sessionId] = segment.EndTicks;

                return;
            }

            _lastSkip.TryRemove(e.Session.Id, out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while processing playback progress.");
        }
    }

    private void SessionManager_PlaybackStopped(object? sender, PlaybackStopEventArgs e)
    {
        if (e.Session != null)
        {
            _lastSkip.TryRemove(e.Session.Id, out _);
        }
    }
}
