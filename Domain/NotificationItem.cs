//A stateful, auditable, immutable-identity representation of work in progress.
/*
 * Key design principles reflected here:

    Immutability where possible (Id, Channel, PayloadJson)

    Controlled mutability where required (State, RetryCount)

    Separation of concerns (domain vs persistence vs transport)

    Auditability and traceability
 * 
 * How this class is used in the system?

            Ingestion:

                1. Creates a NotificationItem

                2. Sets ID, Channel, PayloadJson, MaxRetry

            Worker:

                1. Reads and updates State

                2. Routes valid notifications

                3. Rule Engine

                4. Reads PayloadJson

                5. Produces violations

            Repository:

                1. Persists the entire lifecycle

            Audit :

               1. References Id for traceability
 * 
 */

/*
 * Imports core .NET types.

    Required for:

        Guid

        DateTime
 * 
 */
using System;

/*
 * Places this class in the Domain layer.

    Indicates that NotificationItem represents a business concept, not:

        database schema

        messaging protocol

        infrastructure detail

        Domain objects are intentionally:

        simple

        free of external dependencies

        reusable across layers
 * 
 */
namespace ProcessEngine.Worker.Domain;

/*
 * Defines the central domain entity processed by the worker.

        Represents one logical notification flowing through the system.

        Every ingestion channel (RabbitMQ, file, SFTP, etc.) ultimately produces a NotificationItem.

        This class is the single source of truth for:

        what is processed

        how it is tracked

        how failures are recorded
 * 
 */

public class NotificationItem
{
    //Globally unique identifier for the notification.Generated at ingestion time.Used consistently across:
    //database records, audit logs, rule validation, processing pipelines.
    //init ensures: ID can be set only once, ID cannot be accidentally modified later
    public Guid Id { get; init; }

    //Indicates where the notification came from. Typical values:RabbitMQ, File, SFTP.
    //Stored as an enum to:enforce valid values, avoid string comparison bugs
    //Used for: routing decisions, conditional rule evaluation, audit analysis
    //init ensures the source channel never changes after creation.
    public NotificationChannel Channel { get; init; }

    //Represents the current lifecycle state of the notification. 
    /*
     * Examples:

        New

        Queued

        Processing

        Completed

        FailedTemp

        FailedPerm

        This property is mutable because:

        notifications transition through states

        state changes are persisted to the database

        Acts as a state machine for processing control.
     */
    public NotificationState State { get; set; }


    /*
     * Tracks how many times processing has been attempted.

        Incremented after each failure.

        Used to:

        enforce retry limits

        calculate exponential backoff

        decide between temporary and permanent failure
     * 
     */
    public int RetryCount { get; set; }

    /*
     * Maximum number of retries allowed for this notification.

        Set at creation time and never changed.

        Enables:

        per-notification retry policies

        future rule-based retry customization

        init enforces immutability after creation.
     * 
     */
    public int MaxRetry { get; init; }


    /*
     * Represents the earliest time at which this notification may be retried.

            Nullable because:

            new notifications may not need retry scheduling

            permanently failed notifications do not retry

            Used by the repository to:

            filter eligible notifications

            implement backoff strategies

            Stored as UTC to avoid timezone issues.
     * 
     */
    public DateTime? NextRetryAt { get; set; }

    /*
     * Contains the normalized JSON payload for the notification.

            Stored as raw JSON text to:

            preserve original content

            enable flexible rule evaluation

            support auditing and replay

            Always JSON, regardless of original source format.

            Default empty string avoids null-reference issues.

            init ensures payload immutability after ingestion.

            This property is the primary input for:

            rule validation

            processing logic

            audit logging
     * 
     */

    public string PayloadJson { get; init; } = string.Empty;

    /*
     * Stores the most recent processing error message.

        Nullable because:

        successful notifications have no error

        Used mainly for:

        operational diagnostics

        temporary failure analysis

        Separate from rule violations, which are stored explicitly.
     * 
     */

    public string? LastError { get; set; }
}
