using System;
using System.IO;
using System.Text;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Infrastructure.Audit;

public class TextAuditLogger : IAuditLogger
{
    private readonly string _filePath;

    public TextAuditLogger(string filePath)
    {
        _filePath = filePath;

        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public void Log(AuditEvent e)
    {
        var line = new StringBuilder()
            .Append(e.TimestampUtc.ToString("O")).Append(" | ")
            .Append(e.NotificationId).Append(" | ")
            .Append(e.Stage).Append(" | ")
            .Append(e.Action).Append(" | ")
            .Append(e.Outcome);

        if (!string.IsNullOrWhiteSpace(e.Details))
        {
            line.Append(" | ").Append(e.Details);
        }

        System.IO.File.AppendAllText(
            _filePath,
            line.AppendLine().ToString());
    }
}
