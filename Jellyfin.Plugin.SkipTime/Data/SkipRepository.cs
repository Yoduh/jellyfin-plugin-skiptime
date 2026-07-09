using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.SkipTime.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// SQLite implementation of <see cref="ISkipRepository"/>.
/// </summary>
public sealed class SkipRepository : ISkipRepository
{
    private readonly ILogger<SkipRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipRepository"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for this repository.</param>
    public SkipRepository(ILogger<SkipRepository> logger)
    {
        _logger = logger;
    }

    private string DatabasePath
    {
        get
        {
            ArgumentNullException.ThrowIfNull(Plugin.Instance);

            ArgumentException.ThrowIfNullOrWhiteSpace(
                Plugin.Instance.DatabasePath);

            return Plugin.Instance.DatabasePath;
        }
    }

    private string ConnectionString => $"Data Source={DatabasePath};Cache=Shared";

    /// <summary>
    /// Ensures the SQLite database and schema for skip segments exist.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the initialization operation.</param>
    /// <returns>A task that completes when the database has been initialized.</returns>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);

        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var command = connection.CreateCommand();

        command.CommandText = """
            CREATE TABLE IF NOT EXISTS SkipSegments
            (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ItemId TEXT NOT NULL,
                StartTicks INTEGER NOT NULL,
                EndTicks INTEGER NOT NULL,
                Enabled INTEGER NOT NULL DEFAULT 1,
                CreatedUtc TEXT NOT NULL,
                UpdatedUtc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS
                IX_SkipSegments_ItemId
            ON SkipSegments(ItemId);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Skip Time database initialized.");
    }

    /// <summary>
    /// Retrieves all configured skip segments for the specified media item.
    /// </summary>
    /// <param name="itemId">The media item GUID to retrieve segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of <see cref="SkipSegment"/> for the specified item. The list will be empty when no segments are configured.</returns>
    public async Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var result = new List<SkipSegment>();

        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                ItemId,
                StartTicks,
                EndTicks
            FROM SkipSegments
            WHERE ItemId = $itemId
            ORDER BY StartTicks;
            """;

        command.Parameters.AddWithValue("$itemId", itemId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(
                new SkipSegment
                {
                    Id = reader.GetInt64(0),
                    ItemId = Guid.Parse(reader.GetString(1)),
                    StartTicks = reader.GetInt64(2),
                    EndTicks = reader.GetInt64(3)
                });
        }

        return result;
    }

    /// <summary>
    /// Replaces all configured skip segments for the specified media item.
    /// </summary>
    /// <param name="itemId">The media item GUID to save segments for.</param>
    /// <param name="segments">The collection of <see cref="SkipSegment"/> to persist. Must not be null.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the segments have been saved.</returns>
    public async Task SaveSegmentsAsync(
        Guid itemId,
        IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var normalized = Normalize(itemId, segments);

        await using var connection = new SqliteConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM SkipSegments WHERE ItemId=$itemId";
            delete.Parameters.AddWithValue("$itemId", itemId.ToString());
            await delete.ExecuteNonQueryAsync(cancellationToken);

            foreach (var segment in normalized)
            {
                var insert = connection.CreateCommand();

                insert.CommandText = """
                INSERT INTO SkipSegments
                (
                    ItemId,
                    StartTicks,
                    EndTicks,
                    Enabled,
                    CreatedUtc,
                    UpdatedUtc
                )
                VALUES
                (
                    $itemId,
                    $start,
                    $end,
                    1,
                    $created,
                    $updated
                );
                """;

                insert.Parameters.AddWithValue("$itemId", itemId.ToString());
                insert.Parameters.AddWithValue("$start", segment.StartTicks);
                insert.Parameters.AddWithValue("$end", segment.EndTicks);
                var now = DateTime.UtcNow.ToString("O");
                insert.Parameters.AddWithValue("$created", now);
                insert.Parameters.AddWithValue("$updated", now);

                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Saved {Count} skip segments for {ItemId}.",
                normalized.Count,
                itemId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                "Failed to save skip segments for {ItemId}.",
                itemId);
            throw;
        }
    }

    /// <summary>
    /// Deletes all configured skip segments for the specified media item.
    /// </summary>
    /// <param name="itemId">The media item GUID to delete segments for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the segments have been deleted.</returns>
    public async Task DeleteSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM SkipSegments WHERE ItemId=$itemId";

        command.Parameters.AddWithValue("$itemId", itemId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Determines whether the specified media item has configured skip segments.
    /// </summary>
    /// <param name="itemId">The media item GUID to check for configured segments.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>True if the item has configured segments; otherwise false.</returns>
    public async Task<bool> HasSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM SkipSegments
            WHERE ItemId=$itemId;
            """;

        command.Parameters.AddWithValue("$itemId", itemId.ToString());

        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (result is not null && result != DBNull.Value)
        {
            var count = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
            return count > 0;
        }

        return false;
    }

    /// <summary>
    /// Returns the set of media item IDs that currently have configured skip segments.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A read-only list of GUIDs for media items that have configured skip segments.</returns>
    public async Task<IReadOnlyList<Guid>> GetConfiguredItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<Guid>();

        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText = """
            SELECT DISTINCT ItemId
            FROM SkipSegments
            ORDER BY ItemId;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(Guid.Parse(reader.GetString(0)));
        }

        return result;
    }

    private static List<SkipSegment> Normalize(Guid itemId, IEnumerable<SkipSegment> segments)
    {
        var ordered = segments
            .Select(x =>
            {
                if (x.StartTicks < 0)
                {
                    throw new ArgumentException("StartTicks cannot be negative.");
                }

                if (x.EndTicks <= x.StartTicks)
                {
                    throw new ArgumentException("EndTicks must be greater than StartTicks.");
                }

                return new SkipSegment
                {
                    ItemId = itemId,
                    StartTicks = x.StartTicks,
                    EndTicks = x.EndTicks
                };
            })
            .OrderBy(x => x.StartTicks)
            .ToList();

        if (ordered.Count == 0)
        {
            return ordered;
        }

        var merged = new List<SkipSegment>();

        var current = ordered[0];

        foreach (var next in ordered.Skip(1))
        {
            if (next.StartTicks <= current.EndTicks)
            {
                current.EndTicks = Math.Max(current.EndTicks, next.EndTicks);

                continue;
            }

            merged.Add(current);
            current = next;
        }

        merged.Add(current);

        return merged;
    }
}
