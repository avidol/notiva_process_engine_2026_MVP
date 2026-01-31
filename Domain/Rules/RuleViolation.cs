namespace ProcessEngine.Worker.Domain.Rules;

public record RuleViolation(
    string RuleId,
    string Field,
    string Message
);
