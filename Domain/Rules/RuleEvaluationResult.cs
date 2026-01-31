using System.Collections.Generic;

namespace ProcessEngine.Worker.Domain.Rules;

public record RuleEvaluationResult(
    bool IsValid,
    IReadOnlyList<RuleViolation> Violations
);
