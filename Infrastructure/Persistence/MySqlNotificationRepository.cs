using Dapper;
using ProcessEngine.Worker.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessEngine.Worker.Infrastructure.Persistence;

public class MySqlNotificationRepository : INotificationRepository
{
    private readonly DbConnectionFactory _factory;

    private readonly ISqlProvider _sql;

    public MySqlNotificationRepository(DbConnectionFactory factory, ISqlProvider sql)
    {
        _factory = factory;
        _sql = sql;
    }

    public async Task<IEnumerable<NotificationItem>> FetchPendingAsync()
    {
        using var db = _factory.Create();

        return await db.QueryAsync<NotificationItem>(
            _sql.Get("fetch_pending.sql")
        );

        //return await db.QueryAsync<NotificationItem>(
        //    """
        //    SELECT *
        //    FROM notifications
        //    WHERE state IN (0, 1, 5)   -- New, Queued, FailedTemp
        //      AND (next_retry_at IS NULL OR next_retry_at <= NOW())
        //    ORDER BY created_at
        //    LIMIT 50
        //    """
        //);
    }

    public async Task InsertAsync(NotificationItem item)
    {
        using var db = _factory.Create();
        await db.ExecuteAsync(
            _sql.Get("insert.sql"),
            new
            {
                Id = item.Id.ToString(),
                Channel = (int)item.Channel,
                State = (int)item.State,
                item.RetryCount,
                item.MaxRetry,
                item.PayloadJson
            }

        //await db.ExecuteAsync(
        //    """
        //    INSERT INTO notifications
        //    (id, channel, state, retry_count, max_retry, payload_json)
        //    VALUES
        //    (@Id, @Channel, @State, @RetryCount, @MaxRetry, @PayloadJson)
        //    """,
        //    new
        //    {
        //        Id = item.Id.ToString(),
        //        Channel = (int)item.Channel,
        //        State = (int)item.State,
        //        item.RetryCount,
        //        item.MaxRetry,
        //        item.PayloadJson
        //    }
        );
    }

    public async Task MarkProcessingAsync(Guid id)
    {
        using var db = _factory.Create();
        await db.ExecuteAsync(
            _sql.Get("mark_processing.sql"),
            new
            {
                id = id.ToString(),
                state = (int)NotificationState.Processing
            }
        );

        //await db.ExecuteAsync(
        //    """
        //    UPDATE notifications
        //    SET state = @state
        //    WHERE id = @id
        //    """,
        //    new
        //    {
        //        id = id.ToString(),
        //        state = (int)NotificationState.Processing
        //    }
        //);
    }

    public async Task MarkCompletedAsync(Guid id)
    {
        using var db = _factory.Create();
        await db.ExecuteAsync(
             _sql.Get("mark_completed.sql"),
            new
            {
                id = id.ToString(),
                state = (int)NotificationState.Completed
            }
        );

        //await db.ExecuteAsync(
        //    """
        //    UPDATE notifications
        //    SET state = @state
        //    WHERE id = @id
        //    """,
        //    new
        //    {
        //        id = id.ToString(),
        //        state = (int)NotificationState.Completed
        //    }
        //);
    }

    public async Task MarkFailedAsync(Guid id, string error, bool permanent)
    {
        using var db = _factory.Create();

        await db.ExecuteAsync(
            _sql.Get("mark_failed.sql"),
            new
            {
                id = id.ToString(),
                error,
                permanent = permanent ? 1 : 0,
                state = permanent
                    ? (int)NotificationState.FailedPerm
                    : (int)NotificationState.FailedTemp
            }
        );

        //await db.ExecuteAsync(
        //    """
        //    UPDATE notifications
        //    SET state = @state,
        //        retry_count = retry_count + 1,
        //        next_retry_at =
        //            IF(@permanent = 1,
        //               NULL,
        //               DATE_ADD(NOW(), INTERVAL POW(2, retry_count) SECOND)),
        //        last_error = @error
        //    WHERE id = @id
        //    """,
        //    new
        //    {
        //        id = id.ToString(),
        //        error,
        //        permanent = permanent ? 1 : 0,
        //        state = permanent
        //            ? (int)NotificationState.FailedPerm
        //            : (int)NotificationState.FailedTemp
        //    }
        //);
    }

    public async Task MarkFailedPermAsync(Guid id, string reason)
    {
        using var db = _factory.Create();

        await db.ExecuteAsync(
            _sql.Get("mark_failed_perm.sql"),
            new
            {
                id = id.ToString(),
                state = (int)NotificationState.FailedPerm,
                reason
            }
        );

        //await db.ExecuteAsync(
        //    """
        //    UPDATE notifications
        //    SET state = @state,
        //        rule_violations = @reason
        //    WHERE id = @id
        //    """,
        //    new
        //    {
        //        id = id.ToString(),
        //        state = (int)NotificationState.FailedPerm,
        //        reason
        //    }
        //);
    }
}
