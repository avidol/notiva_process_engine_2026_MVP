using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Processing;
using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Infrastructure.Processing;

public sealed class PdfGenerationStep : IProcessingStep
{
    public string Name => "PDF_GENERATION";

    private readonly IConfiguration _config;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<PdfGenerationStep> _logger;

    public PdfGenerationStep(
        IConfiguration config,
        IAuditLogger auditLogger,
        ILogger<PdfGenerationStep> logger)
    {
        _config = config;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Artifacts.TryGetValue("XML", out var xmlObj))
        {
            _logger.LogInformation("No XML artifact found. Skipping PDF step.");
            return;
        }

        var xml = xmlObj.ToString()!;

        try
        {
            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "START",
                Details = "Generating PDF from XML"
            });

            var pdfBytes = GeneratePdf(xml);

            context.Artifacts["PDF"] = pdfBytes;

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "SUCCESS",
                Details = "PDF generated successfully"
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

    private static byte[] GeneratePdf(string xml)
    {
        using var stream = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(30);

                page.Content().Column(col =>
                {
                    col.Item().Text("Notification PDF")
                        .FontSize(18)
                        .Bold();

                    col.Item().Text("Generated from XML:")
                        .Italic();

                    col.Item().Text(xml)
                        .FontSize(10)
                        .FontFamily(Fonts.Courier);
                });
            });
        }).GeneratePdf(stream);

        return stream.ToArray();
    }
}
