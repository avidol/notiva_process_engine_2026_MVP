namespace ProcessEngine.Worker.Domain.Rules;

public class RuleDefinition
{
    public string RuleId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // required, length, enum
    public string Field { get; set; } = string.Empty;

    // Optional properties
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string[]? Values { get; set; }
    public string? Message { get; set; }
}
