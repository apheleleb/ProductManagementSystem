using Microsoft.Extensions.Logging;

namespace CatalystPMS.Infrastructure.ExternalServices;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string body)
    {
        // Stub: in production wire up SendGrid / SMTP here.
        // Notifications in the PMS are in-app; this exists for future email delivery.
        _logger.LogInformation(
            "Email stub — To: {To} | Subject: {Subject}", toEmail, subject);

        return Task.CompletedTask;
    }
}