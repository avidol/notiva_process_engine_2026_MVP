using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Application.Processing;

namespace ProcessEngine.Worker.Infrastructure.Processing;

public sealed class PdfFileOutputStep : IProcessingStep
{
    public string Name => "PDF_FILE_OUTPUT";

    private readonly string _outputDir;
    private readonly ILogger<PdfFileOutputStep> _logger;

    public PdfFileOutputStep(
        IConfiguration config,
        ILogger<PdfFileOutputStep> logger)
    {
        _logger = logger;

        _outputDir = config["ProcessingSteps:PdfOutput:Directory"]
            ?? "output/pdf";

        Directory.CreateDirectory(_outputDir);
    }

    public async Task ExecuteAsync(
        ProcessingContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Artifacts.TryGetValue("PDF", out var pdfObj))
            return; // PDF step disabled or skipped

        var pdfBytes = (byte[])pdfObj;

        var filePath = Path.Combine(
            _outputDir,
            $"{context.NotificationId}.pdf");

        await System.IO.File.WriteAllBytesAsync(
            filePath,
            pdfBytes,
            cancellationToken);

        _logger.LogInformation(
            "PDF saved to {Path}",
            filePath);
    }
}
