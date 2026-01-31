using System;

namespace ProcessEngine.Worker.Domain.Audit;

public class AuditEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public Guid NotificationId { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    public string Stage { get; init; } = string.Empty;   // RULE_ENGINE
    public string Action { get; init; } = string.Empty;  // VALIDATE
    public string Outcome { get; init; } = string.Empty; // PASS / FAIL

    public string? Details { get; init; }
}
