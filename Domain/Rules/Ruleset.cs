using System.Collections.Generic;

namespace ProcessEngine.Worker.Domain.Rules;

public class Ruleset
{
    public string RulesetId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<RuleDefinition> Rules { get; set; } = new();
}
