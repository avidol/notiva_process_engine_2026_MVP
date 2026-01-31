using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Domain.Audit;
using ProcessEngine.Worker.Domain.Rules;
using ProcessEngine.Worker.Infrastructure.Rules;

namespace ProcessEngine.Worker.Application.Rules;

public class RuleValidator : IRuleValidator
{
    private readonly FileRulesetProvider _rulesetProvider;
    private readonly IAuditLogger _auditLogger;

    public RuleValidator(
        FileRulesetProvider rulesetProvider,
        IAuditLogger auditLogger)
    {
        _rulesetProvider = rulesetProvider;
        _auditLogger = auditLogger;
    }

    public RuleEvaluationResult Validate(RuleContext context)
    {
        var ruleset = _rulesetProvider.GetRuleset();
        var violations = new List<RuleViolation>();

        JsonElement envelope;
        JsonElement businessPayload;

        // -------------------------------
        // 1️⃣ Parse outer envelope JSON
        // -------------------------------
        try
        {
            envelope = JsonSerializer.Deserialize<JsonElement>(context.PayloadJson);
        }
        catch (Exception ex)
        {
            return FailInvalidJson(
                context,
                violations,
                $"Envelope JSON is invalid: {ex.Message}"
            );
        }

        // ---------------------------------------
        // 2️⃣ Extract raw payload from envelope
        // ---------------------------------------
        if (!envelope.TryGetProperty("raw", out var rawElement))
        {
            return FailInvalidJson(
                context,
                violations,
                "Missing 'raw' field in payload envelope"
            );
        }

        if (rawElement.ValueKind != JsonValueKind.String)
        {
            return FailInvalidJson(
                context,
                violations,
                "'raw' field must be a string"
            );
        }

        var rawPayload = rawElement.GetString();

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            return FailInvalidJson(
                context,
                violations,
                "Raw payload is empty"
            );
        }

        // ---------------------------------------
        // 3️⃣ Parse business payload JSON
        // ---------------------------------------
        try
        {
            businessPayload = JsonSerializer.Deserialize<JsonElement>(rawPayload);
        }
        catch (Exception ex)
        {
            return FailInvalidJson(
                context,
                violations,
                $"Business payload is not valid JSON: {ex.Message}"
            );
        }

        if (businessPayload.ValueKind != JsonValueKind.Object)
        {
            return FailInvalidJson(
                context,
                violations,
                "Business payload must be a JSON object"
            );
        }

        // ---------------------------------------
        // 4️⃣ Convert business payload to dictionary
        // ---------------------------------------
        var payloadDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
            businessPayload.GetRawText()
        ) ?? new Dictionary<string, object>();

        // ---------------------------------------
        // 5️⃣ Apply rules
        // ---------------------------------------
        foreach (var rule in ruleset.Rules)
        {
            ApplyRule(rule, payloadDict, violations);
        }

        var result = new RuleEvaluationResult(
            violations.Count == 0,
            violations
        );

        LogAudit(context.NotificationId, violations);

        return result;
    }

    // =====================================================
    // Rule application logic (unchanged, but now correct)
    // =====================================================
    private void ApplyRule(
        RuleDefinition rule,
        Dictionary<string, object> payload,
        List<RuleViolation> violations)
    {
        switch (rule.Type)
        {
            case "required":
                if (!payload.ContainsKey(rule.Field) || payload[rule.Field] == null)
                {
                    violations.Add(new RuleViolation(
                        rule.RuleId,
                        rule.Field,
                        rule.Message ?? "Field is required"
                    ));
                }
                break;

            case "length":
                if (!payload.ContainsKey(rule.Field))
                    break;

                var value = payload[rule.Field]?.ToString();
                if (string.IsNullOrEmpty(value))
                    break;

                if ((rule.Min.HasValue && value.Length < rule.Min.Value) ||
                    (rule.Max.HasValue && value.Length > rule.Max.Value))
                {
                    violations.Add(new RuleViolation(
                        rule.RuleId,
                        rule.Field,
                        $"Length must be between {rule.Min} and {rule.Max}"
                    ));
                }
                break;

            case "enum":
                if (!payload.ContainsKey(rule.Field))
                    break;

                var enumValue = payload[rule.Field]?.ToString();
                if (enumValue == null)
                    break;

                if (rule.Values == null || !rule.Values.Contains(enumValue))
                {
                    violations.Add(new RuleViolation(
                        rule.RuleId,
                        rule.Field,
                        $"Invalid value '{enumValue}'"
                    ));
                }
                break;

            default:
                violations.Add(new RuleViolation(
                    rule.RuleId,
                    rule.Field,
                    $"Unsupported rule type '{rule.Type}'"
                ));
                break;
        }
    }

    // =====================================================
    // Shared failure helper
    // =====================================================
    private RuleEvaluationResult FailInvalidJson(
        RuleContext context,
        List<RuleViolation> violations,
        string message)
    {
        violations.Add(new RuleViolation(
            "INVALID_JSON",
            "$",
            message
        ));

        LogAudit(context.NotificationId, violations);

        return new RuleEvaluationResult(false, violations);
    }

    // =====================================================
    // Audit logging
    // =====================================================
    private void LogAudit(Guid notificationId, List<RuleViolation> violations)
    {
        var auditEvent = new AuditEvent
        {
            NotificationId = notificationId,
            Stage = "RULE_ENGINE",
            Action = "VALIDATE",
            Outcome = violations.Count == 0 ? "PASS" : "FAIL",
            Details = violations.Count == 0
                ? "All rules passed"
                : string.Join("; ",
                    violations.Select(v =>
                        $"{v.RuleId}:{v.Field}:{v.Message}"))
        };

        _auditLogger.Log(auditEvent);
    }
}
