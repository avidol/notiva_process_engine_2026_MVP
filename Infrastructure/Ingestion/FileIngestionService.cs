using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ProcessEngine.Worker.Domain;
using ProcessEngine.Worker.Infrastructure.Persistence;

namespace ProcessEngine.Worker.Infrastructure.Ingestion;

public class FileIngestionService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<FileIngestionService> _logger;
    private readonly string _inputDir;
    private readonly string _archiveDir;

    public FileIngestionService(
        INotificationRepository repository,
        IConfiguration config,
        ILogger<FileIngestionService> logger)
    {
        _repository = repository;
        _logger = logger;

        _inputDir = config["FileIngestion:InputDirectory"]!;
        _archiveDir = config["FileIngestion:ArchiveDirectory"]!;
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_inputDir);
        Directory.CreateDirectory(_archiveDir);

        foreach (var file in Directory.GetFiles(_inputDir))
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var rawContent = await System.IO.File.ReadAllTextAsync(file, ct);

                var payload = new
                {
                    FileName = Path.GetFileName(file),
                    IngestedAt = DateTime.UtcNow,
                    Content = rawContent
                };

                var notification = new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    Channel = NotificationChannel.File,
                    State = NotificationState.New,
                    RetryCount = 0,
                    MaxRetry = 5,
                    PayloadJson = System.Text.Json.JsonSerializer.Serialize(payload)
                };

                await _repository.InsertAsync(notification);

                var archivePath = Path.Combine(
                    _archiveDir,
                    Path.GetFileName(file)
                );

                System.IO.File.Move(file, archivePath, overwrite: true);

                _logger.LogInformation($"File ingested: {file}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to ingest file {file}");
            }
        }
    }
}
