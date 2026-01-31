UPDATE notifications
SET state = @state,
    retry_count = retry_count + 1,
    next_retry_at =
        IF(@permanent = 1,
           NULL,
           DATE_ADD(NOW(), INTERVAL POW(2, retry_count) SECOND)),
    last_error = @error
WHERE id = @id;