/*
 * Places this enum in the Domain layer.

        Indicates that the source of a notification is a business concept, not an infrastructure detail.

        By keeping it in the domain: rules, processors, audit logic
        can reason about channels without knowing how they are implemented.
 * 
 * Enum declaration:
 * Defines the origin channel from which a NotificationItem was ingested.

        Encapsulates where the data came from, not how it was transported.

        Used throughout the system for:

        routing decisions

        rule evaluation

        auditing

        reporting

    Storing this as an enum ensures:

        only valid channels are used

        no string-based errors

        compact storage in the database (integer)
 * 
 * File: 

        Indicates the notification originated from a file-based ingestion source.

        This may represent:

        local file system polling

        SFTP downloads

        batch file drops

        Even though SFTP or local file ingestion may differ technically,
        they can still map to the same logical business channel if desired.

        This abstraction allows you to:

        treat multiple ingestion mechanisms as one logical channel

        simplify rule definitions

        avoid coupling rules to transport details
    As the system evolves, additional channels can be added safely, for example:
 */

namespace ProcessEngine.Worker.Domain;

public enum NotificationChannel
{
    File
}
