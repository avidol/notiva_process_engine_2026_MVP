/*
 * INotificationRepository defines the persistence contract for notification lifecycle management, 
 * encapsulating retrieval, state transitions, retry handling, and failure recording while 
 * shielding higher layers from database-specific concerns.
 * 
 * INotificationRepository is designed to:

        isolate persistence logic

        centralize state transitions

        keep business logic database-agnostic
 * 
 * Imports the Domain layer, because the repository works with NotificationItem.

        Uses core .NET namespaces:

            System → Guid

            System.Collections.Generic → collections

            System.Threading.Tasks → asynchronous operations

            All repository methods are asynchronous to support:

            scalability

            non-blocking I/O

            long-running worker stability
 * 
 * 
 */


using ProcessEngine.Worker.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/*
 * Places this interface in the Infrastructure.Persistence layer.

        Indicates this abstraction is about data storage, not business rules.

        Even though it lives in Infrastructure, it depends only on:

        domain entities

        standard .NET types

        This allows the Application and Worker layers to depend on the interface without knowing:

        database type

        SQL dialect

        ORM choice
 * 
 * 
 */
namespace ProcessEngine.Worker.Infrastructure.Persistence;

/*
 * Defines the contract for persisting and retrieving NotificationItem objects.

        This is a classic Repository Pattern implementation.

        The interface ensures:

        consistent persistence behavior

        easy substitution of implementations (MySQL, PostgreSQL, mock, etc.)

        testability of higher layers
 * 
 */

public interface INotificationRepository
{
    Task<IEnumerable<NotificationItem>> FetchPendingAsync();
    /*
     * Retrieves notifications that are eligible for processing.

            Typically includes notifications in states such as:

            New

            Queued

            FailedTemp (retryable)

            Encapsulates all database filtering logic.

            The Worker does not need to know:

            SQL

            retry rules

            time-based conditions

            This method makes the database the source of truth for work scheduling.
     * 
     */


    Task InsertAsync(NotificationItem item);
    /*
     * Persists a newly ingested NotificationItem.

        Called by ingestion services (RabbitMQ, file, SFTP).

        Stores:

        payload

        channel

        initial state

        retry configuration

        This method represents the entry point of notifications into the system.
     *  
     */
    Task MarkProcessingAsync(Guid id);
    /*
     * Updates a notification’s state to Processing.

        Called just before actual business processing begins.

        Prevents:

        duplicate processing

        concurrent handling of the same notification

        Supports idempotency and concurrency control.
     * 
     */
    Task MarkCompletedAsync(Guid id);
    /*
     * Marks a notification as successfully completed.

            Called after:

            processing finishes

            all required downstream actions succeed

            Transitions the notification into a terminal success state.
     * 
     */
    Task MarkFailedAsync(Guid id, string error, bool permanent);
    /*
     * Records a processing failure.

    Accepts:

        an error message

        a flag indicating whether the failure is permanent

        Based on the permanent flag, the repository implementation can:

        schedule retries

        increment retry counters

        or mark the notification as permanently failed

        This method supports retry-aware failure handling.
     * 
     */
    Task MarkFailedPermAsync(Guid id, string reason);
    /*
     * Marks a notification as permanently failed.

            Typically used when:

            rule validation fails

            payload is invalid

            business rules reject the notification

            Stores a structured reason (often JSON) explaining why the notification failed.

            This enables:

            auditability

            transparency

            operational debugging
     * 
     */
}
