using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ProcessEngine.Worker.Application.Processing;

public sealed class ProcessingContext
{
    public Guid NotificationId { get; }
    public JsonDocument Payload { get; }

    // Artifacts produced by steps (XML, PDF, etc.)
    public IDictionary<string, object> Artifacts { get; }

    public ProcessingContext(Guid notificationId, JsonDocument payload)
    {
        NotificationId = notificationId;
        Payload = payload;
        Artifacts = new Dictionary<string, object>();
    }
}
