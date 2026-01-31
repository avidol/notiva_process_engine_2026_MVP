using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ProcessEngine.Worker.Domain;
using ProcessEngine.Worker.Infrastructure.Persistence;
using ProcessEngine.Worker.Infrastructure.File;

namespace ProcessEngine.Worker.Application;

public class NotificationProcessor : INotificationProcessor
{
    private readonly INotificationRepository _repo;
    private readonly FileNotificationHandler _fileHandler;
    private readonly ILogger<NotificationProcessor> _logger;

    public NotificationProcessor(
        INotificationRepository repo,
        FileNotificationHandler fileHandler,
        ILogger<NotificationProcessor> logger)
    {
        _repo = repo;
        _fileHandler = fileHandler;
        _logger = logger;
    }

    public async Task ProcessAsync(NotificationItem n, CancellationToken ct)
    {
        try
        {
            await _repo.MarkProcessingAsync(n.Id);

            await _fileHandler.HandleAsync(n);

            await _repo.MarkCompletedAsync(n.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed");

            bool permanent = n.RetryCount >= n.MaxRetry;
            await _repo.MarkFailedAsync(n.Id, ex.Message, permanent);
        }
    }
}
