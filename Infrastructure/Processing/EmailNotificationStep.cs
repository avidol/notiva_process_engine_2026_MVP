using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ProcessEngine.Worker.Application.Audit;
using ProcessEngine.Worker.Application.Processing;
using ProcessEngine.Worker.Domain.Audit;

namespace ProcessEngine.Worker.Infrastructure.Processing;

public sealed class EmailNotificationStep : IProcessingStep
{
    public string Name => "EMAIL_NOTIFICATION";

    private readonly IConfiguration _config;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<EmailNotificationStep> _logger;

    public EmailNotificationStep(
        IConfiguration config,
        IAuditLogger auditLogger,
        ILogger<EmailNotificationStep> logger)
    {
        _config = config;
        _auditLogger = auditLogger;
        _logger = logger;
    }

    public async Task ExecuteAsync(
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
                Details = "Sending email notification"
            });

            var smtp = _config.GetSection("ProcessingSteps:Email:Smtp");

            var message = new MailMessage
            {
                From = new MailAddress(smtp["From"]!),
                Subject = "Process Engine Notification",
                Body = "Your message has been processed successfully.",
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(smtp["To"]!));

            // --------------------------------------------------
            // 📎 Attach PDF if available
            // --------------------------------------------------
            if (context.Artifacts.TryGetValue("PDF", out var pdfObj)
                && pdfObj is byte[] pdfBytes)
            {
                var stream = new MemoryStream(pdfBytes);

                var attachment = new Attachment(
                    stream,
                    "notification.pdf",
                    "application/pdf");

                message.Attachments.Add(attachment);

                _logger.LogInformation(
                    "PDF attached to email for notification {Id}",
                    context.NotificationId);
            }
            else
            {
                _logger.LogWarning(
                    "No PDF found to attach for notification {Id}",
                    context.NotificationId);
            }

            using var smtpClient = new SmtpClient(
                smtp["Host"],
                smtp.GetValue<int>("Port"))
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    smtp["Username"],
                    smtp["Password"])
            };

            await smtpClient.SendMailAsync(message, cancellationToken);

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "SUCCESS",
                Details = "Email sent successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email sending failed");

            _auditLogger.Log(new AuditEvent
            {
                NotificationId = context.NotificationId,
                Stage = Name,
                Action = "EXECUTE",
                Outcome = "FAIL",
                Details = ex.Message
            });

            // NON-FATAL step
        }
    }
}
