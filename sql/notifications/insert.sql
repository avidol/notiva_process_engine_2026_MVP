INSERT INTO notifications
(id, channel, state, retry_count, max_retry, payload_json)
VALUES
(@Id, @Channel, @State, @RetryCount, @MaxRetry, @PayloadJson);