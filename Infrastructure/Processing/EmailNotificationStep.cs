using System;
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

            // --------------------------------------------------
            // Read SMTP configuration (STRONGLY typed keys)
            // --------------------------------------------------
            var smtpSection = _config.GetSection("ProcessingSteps:Email:Smtp");

            var host = smtpSection["Host"]
                ?? throw new InvalidOperationException("SMTP Host not configured");

            var port = smtpSection.GetValue<int>("Port");
            var username = smtpSection["Username"]
                ?? throw new InvalidOperationException("SMTP Username not configured");

            var password = smtpSection["Password"]
                ?? throw new InvalidOperationException("SMTP Password not configured");

            var from = smtpSection["From"]
                ?? throw new InvalidOperationException("SMTP From not configured");

            var to = smtpSection["To"]
                ?? throw new InvalidOperationException("SMTP To not configured");

            // --------------------------------------------------
            // Build Mail Message
            // --------------------------------------------------
            using var message = new MailMessage
            {
                From = new MailAddress(from),
                Subject = "Process Engine Notification",
                Body = "Your message has been processed successfully.",
                IsBodyHtml = false
            };

            message.To.Add(new MailAddress(to));

            // --------------------------------------------------
            // Configure SMTP client (GMAIL SAFE CONFIG)
            // --------------------------------------------------
            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = true,                     // REQUIRED for Gmail
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    username,
                    password
                ),
                DeliveryMethod = SmtpDeliveryMethod.Network
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

            // IMPORTANT:
            // Email is NON-FATAL
            // Do NOT throw → pipeline continues
        }
    }
}
