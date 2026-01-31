using ProcessEngine.Worker.Domain.Rules;

namespace ProcessEngine.Worker.Application.Rules;

public interface IRuleValidator
{
    RuleEvaluationResult Validate(RuleContext context);
}
