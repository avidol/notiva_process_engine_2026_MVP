using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Processing;
using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Infrastructure.Processing;

public sealed class FileOutputStep : IProcessingStep
{
    public string Name => "FILE_OUTPUT";

    private readonly ILogger<FileOutputStep> _logger;
    private readonly IAuditLogger _auditLogger;

    public FileOutputStep(
        ILogger<FileOutputStep> logger,
        IAuditLogger auditLogger)
    {
        _logger = logger;
        _auditLogger = auditLogger;
    }

    public async Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        var outputDir = "output";
        Directory.CreateDirectory(outputDir);

        var outputPath = Path.Combine(
            outputDir,
            $"{context.NotificationId}.json");

        try
        {
            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "START",
                Details = "Writing JSON payload to file"
            });

            // Serialize JsonDocument back to formatted JSON
            var json = JsonSerializer.Serialize(
                context.Payload.RootElement,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

            await System.IO.File.WriteAllTextAsync(
                outputPath,
                json,
                Encoding.UTF8,
                cancellationToken);

            // Register artifact
            context.Artifacts["FILE_OUTPUT_PATH"] = outputPath;

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "SUCCESS",
                Details = $"Payload written to {outputPath}"
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
    }
}
