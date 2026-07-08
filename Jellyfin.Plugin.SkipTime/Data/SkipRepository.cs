using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SkipTime.Data;

/// <summary>
/// SQLite implementation of <see cref="ISkipRepository"/>.
/// </summary>
public sealed class SkipRepository : SkipTime.Interfaces.ISkipRepository
{
    private readonly string _databasePath;
    private readonly ILogger<SkipRepository> _logger;

    public SkipRepository(SkipTimePaths paths, ILogger<SkipRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(paths);

        ArgumentException.ThrowIfNullOrWhiteSpace(paths.DatabasePath);

        _databasePath = paths.DatabasePath;
        _logger = logger;
    }

    private string ConnectionString => $"Data Source={_databasePath};Cache=Shared";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_databasePath)!);

        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

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

    public async Task<IReadOnlyList<SkipSegment>> GetSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    )
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
                }
            );
        }

        return result;
    }

    public async Task SaveSegmentsAsync(
        Guid itemId,
        IReadOnlyCollection<SkipSegment> segments,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(segments);

        var normalized = Normalize(itemId, segments);

        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var delete = connection.CreateCommand();
        delete.Transaction = transaction;

        delete.CommandText = "DELETE FROM SkipSegments WHERE ItemId=$itemId";

        delete.Parameters.AddWithValue("$itemId", itemId.ToString());

        await delete.ExecuteNonQueryAsync(cancellationToken);

        foreach (var segment in normalized)
        {
            var insert = connection.CreateCommand();

            insert.Transaction = transaction;

            insert.CommandText = """
                INSERT INTO SkipSegments
                (
                    ItemId,
                    StartTicks,
                    EndTicks
                )
                VALUES
                (
                    $itemId,
                    $start,
                    $end
                );
                """;

            insert.Parameters.AddWithValue("$itemId", itemId.ToString());

            insert.Parameters.AddWithValue("$start", segment.StartTicks);

            insert.Parameters.AddWithValue("$end", segment.EndTicks);

            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation(
            "Saved {Count} skip segments for {ItemId}.",
            normalized.Count,
            itemId
        );
    }

    public async Task DeleteSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    )
    {
        await using var connection = new SqliteConnection(ConnectionString);

        await connection.OpenAsync(cancellationToken);

        var command = connection.CreateCommand();

        command.CommandText = "DELETE FROM SkipSegments WHERE ItemId=$itemId";

        command.Parameters.AddWithValue("$itemId", itemId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasSegmentsAsync(
        Guid itemId,
        CancellationToken cancellationToken = default
    )
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

        var count = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

        return count > 0;
    }

    public async Task<IReadOnlyList<Guid>> GetConfiguredItemsAsync(
        CancellationToken cancellationToken = default
    )
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
