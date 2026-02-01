using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Domain;
using ProcessEngine.Worker.Application.Processing;

namespace ProcessEngine.Worker.Application;

public class NotificationProcessor : INotificationProcessor
{
    private readonly ProcessingPipeline _pipeline;
    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(
        ProcessingPipeline pipeline,
        ILogger<NotificationProcessor> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    public async Task ProcessAsync(
        NotificationItem notification,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing notification {NotificationId}",
            notification.Id);

        // Parse persisted JSON payload into a document
        using var payloadDocument =
            JsonDocument.Parse(notification.PayloadJson);

        // Create processing context shared across steps
        var context = new ProcessingContext(
            notification.Id,
            payloadDocument);

        // Execute pipeline (may have zero, one, or many steps)
        await _pipeline.ExecuteAsync(context, cancellationToken);
    }
}
