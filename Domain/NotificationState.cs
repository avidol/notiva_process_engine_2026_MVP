/*
 * Places this enum in the Domain layer.

        Confirms that NotificationState represents a business concept, not a technical detail.

        Domain enums are intentionally:

            simple

            stable

            shared across layers (Worker, Repository, Rules, Processor)
 * 
 */

namespace ProcessEngine.Worker.Domain;

/*
 * Defines the lifecycle states of a NotificationItem.

        This enum acts as a state machine controlling:

        processing eligibility

        retry behavior

        failure handling

        Stored as an integer in the database for performance and reliability.
 * 
 * 
 */

public enum NotificationState
{
    //Initial state of every newly ingested notification. Assigned immediately after ingestion (RabbitMQ, file, SFTP).
    //Indicates: payload is stored, rules have not yet been evaluated, processing has not begun. 
    //This is the entry point into the processing lifecycle.
    New,
    /*
     * Indicates the notification has:

            passed validation

            been accepted for processing

            placed into an internal queue

            Used to decouple:

            rule validation

            actual processing execution

            This state supports asynchronous, parallel processing.
     * 
     */
    Queued,
    /*
     * Indicates active processing is underway.

            Set just before invoking the business processor.

            Prevents:

            duplicate processing

            concurrent handling of the same notification

            Important for idempotency and concurrency control.
     * 
     */
    Processing,
    /*
     * Represents a successful outbound operation.

            Typically used when:

            a notification has been dispatched

            but final confirmation or completion is pending

            Useful for:

            multi-step workflows

            integrations requiring acknowledgements

            This state allows finer-grained tracking than a simple “completed”.
     * 
     */
    Sent,
    /*
     * Terminal success state.

            Indicates the notification has:

            passed validation

            been processed successfully

            completed all required steps

            No further action is taken on notifications in this state.
     * 
     */
    Completed,
    /*
     * Represents a temporary failure.

            Typical causes:

            transient network issues

            downstream system unavailability

            timeouts

            Notifications in this state:

            are eligible for retry

            may transition back to Queued or Processing

            Retry logic uses:

            RetryCount

            NextRetryAt 
     * 
     */
    FailedTemp,
    /*
     * Represents a permanent failure.

            Typical causes:

            rule validation failures

            invalid payload structure

            unrecoverable business errors

            Notifications in this state:

            will never be retried

            are considered terminal failures

            Rule violations are stored and auditable.
     * 
     */
    FailedPerm,
    /*
     * Final fallback state for notifications that:

        exceed retry limits

        cannot be processed safely

        Analogous to a Dead Letter Queue (DLQ).

        Used for:

        manual inspection

        replay or remediation

        operational analysis
     * 
     */
    DeadLetter
}
