using System;

namespace ProcessEngine.Worker.Domain.Rules;

public record RuleContext(
    Guid NotificationId,
    string PayloadJson
);
