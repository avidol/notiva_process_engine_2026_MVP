using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using ProcessEngine.Worker.Domain;
using ProcessEngine.Worker.Infrastructure.Persistence;

namespace Notiva.Notification.Worker.Infrastructure.Ingestion;

public class SftpIngestionService
{
    private readonly INotificationRepository _repository;
    private readonly ILogger<SftpIngestionService> _logger;
    private readonly IConfiguration _config;

    public SftpIngestionService(
        INotificationRepository repository,
        IConfiguration config,
        ILogger<SftpIngestionService> logger)
    {
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    public async Task ScanAsync(CancellationToken ct)
    {
        if (_config["SftpIngestion:Enabled"] != "true")
            return;

        var host = _config["SftpIngestion:Host"]!;
        var port = int.Parse(_config["SftpIngestion:Port"]!);
        var user = _config["SftpIngestion:Username"]!;
        var pass = _config["SftpIngestion:Password"]!;
        var inputDir = _config["SftpIngestion:InputDirectory"]!;
        var archiveDir = _config["SftpIngestion:ArchiveDirectory"]!;

        using var client = new SftpClient(host, port, user, pass);
        client.Connect();

        foreach (var file in client.ListDirectory(inputDir))
        {
            if (ct.IsCancellationRequested)
                break;

            if (file.IsDirectory || file.IsSymbolicLink)
                continue;

            try
            {
                using var ms = new MemoryStream();
                client.DownloadFile(file.FullName, ms);
                var content = Encoding.UTF8.GetString(ms.ToArray());

                var payload = new
                {
                    FileName = file.Name,
                    Source = "SFTP",
                    IngestedAt = DateTime.UtcNow,
                    Content = content
                };

                var notification = new NotificationItem
                {
                    Id = Guid.NewGuid(),
                    Channel = NotificationChannel.File,
                    State = NotificationState.New,
                    RetryCount = 0,
                    MaxRetry = 5,
                    PayloadJson = JsonSerializer.Serialize(payload)
                };

                await _repository.InsertAsync(notification);

                var archivePath = $"{archiveDir}/{file.Name}";
                client.RenameFile(file.FullName, archivePath);

                _logger.LogInformation($"SFTP file ingested: {file.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to ingest SFTP file {file.Name}");
            }
        }

        client.Disconnect();
    }
}
