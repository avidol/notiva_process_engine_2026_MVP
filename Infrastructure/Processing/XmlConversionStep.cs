using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
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

    private readonly IConfiguration _config;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<XmlConversionStep> _logger;

    public XmlConversionStep(
        IConfiguration config,
        IAuditLogger auditLogger,
        ILogger<XmlConversionStep> logger)
    {
        _config = config;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        var rootName =
            _config.GetValue<string>("ProcessingSteps:XmlConversion:RootElement")
            ?? "Root";

        var validate =
            _config.GetValue<bool>("ProcessingSteps:XmlConversion:Validate");

        var xsdPath =
            _config.GetValue<string>("ProcessingSteps:XmlConversion:XsdPath");

        try
        {
            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "START",
                Details = "Converting JSON to XML"
            });

            // Convert JSON → XML
            var xml = ConvertJsonToXml(context.Payload.RootElement, rootName);

            // Optional XSD validation
            if (validate && !string.IsNullOrWhiteSpace(xsdPath))
            {
                ValidateAgainstXsd(xml, xsdPath);
            }

            // Store artifact
            context.Artifacts["XML"] = xml;

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "SUCCESS",
                Details = "XML generated successfully"
            });
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

        await Task.CompletedTask;
    }

    // -------------------- Helpers --------------------

    private static string ConvertJsonToXml(JsonElement json, string rootName)
    {
        var root = new XElement(rootName);
        PopulateXml(root, json);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            root);

        return doc.ToString();
    }

    private static void PopulateXml(XElement parent, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var child = new XElement(prop.Name);
                    PopulateXml(child, prop.Value);
                    parent.Add(child);
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var child = new XElement("Item");
                    PopulateXml(child, item);
                    parent.Add(child);
                }
                break;

            default:
                parent.Value = element.ToString();
                break;
        }
    }

    private static void ValidateAgainstXsd(string xml, string xsdPath)
    {
        var schemas = new XmlSchemaSet();
        schemas.Add(null, xsdPath);

        var doc = XDocument.Parse(xml);

        doc.Validate(schemas, (o, e) =>
        {
            throw new XmlSchemaValidationException(e.Message);
        });
    }
}
