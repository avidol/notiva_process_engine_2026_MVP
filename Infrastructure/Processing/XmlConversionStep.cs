using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Schema;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Processing;
using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Infrastructure.Processing;

public sealed class XmlConversionStep : IProcessingStep
{
    public string Name => "XML_CONVERSION";

    private readonly string _xsdPath;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<XmlConversionStep> _logger;

    public XmlConversionStep(
        IConfiguration configuration,
        IAuditLogger auditLogger,
        ILogger<XmlConversionStep> logger)
    {
        _auditLogger = auditLogger;
        _logger = logger;

        _xsdPath = configuration["ProcessingSteps:XmlConversion:XsdPath"]
            ?? throw new InvalidOperationException(
                "ProcessingSteps:XmlConversion:XsdPath is not configured");
    }

    public Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "START",
                Details = "Converting JSON to canonical XML"
            });

            JsonElement root = context.Payload.RootElement;

            // --------------------------------------------------
            // STEP 1: Unwrap RabbitMQ envelope
            // --------------------------------------------------
            if (root.TryGetProperty("Content", out var content))
            {
                root = content;
            }

            // --------------------------------------------------
            // STEP 2: Unwrap file ingestion "raw" payload
            // --------------------------------------------------
            if (root.TryGetProperty("raw", out var rawElement)
                && rawElement.ValueKind == JsonValueKind.String)
            {
                root = JsonDocument
                    .Parse(rawElement.GetString()!)
                    .RootElement;
            }

            // --------------------------------------------------
            // STEP 3: Extract required business field
            // --------------------------------------------------
            if (!root.TryGetProperty("policyNumber", out var policy))
            {
                throw new InvalidOperationException(
                    "Required field 'policyNumber' is missing in payload");
            }

            // --------------------------------------------------
            // STEP 4: Build canonical XML
            // --------------------------------------------------
            var xml = new XElement("Message",
                new XElement("policyNumber", policy.GetString())
            );

            ValidateAgainstXsd(xml, _xsdPath);

            // --------------------------------------------------
            // STEP 5: Store XML artifact
            // --------------------------------------------------
            context.Artifacts["XML"] =
                xml.ToString(SaveOptions.DisableFormatting);

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "SUCCESS",
                Details = "XML generated and validated"
            });

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "FAIL",
                Details = ex.Message
            });

            throw;
        }
    }



    // ============================================================
    // XSD VALIDATION (EXPLICIT CALL – NO EXTENSION RESOLUTION BUG)
    // ============================================================
    private static void ValidateAgainstXsd(XElement element, string xsdPath)
    {
        var schemas = new XmlSchemaSet();
        schemas.Add(null, xsdPath);

        // Wrap XElement in XDocument (required by this overload)
        var document = new XDocument(element);

        System.Xml.Schema.Extensions.Validate(
            document,
            schemas,
            (sender, args) =>
            {
                throw new XmlSchemaValidationException(args.Message);
            },
            addSchemaInfo: true
        );
    }

    // ============================================================
    // AUDIT HELPER
    // ============================================================
    private void LogAudit(Guid notificationId, string outcome, string details)
    {
        _auditLogger.Log(new AuditEvent
        {
            NotificationId = notificationId,
            Stage = Name,
            Action = "EXECUTE",
            Outcome = outcome,
            Details = details
        });
    }
}
