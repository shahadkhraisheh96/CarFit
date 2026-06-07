using Microsoft.AspNetCore.Identity.UI.Services;

namespace CarFitProject.Services
{
    /// <summary>
    /// Development-only email sender. Writes the email body — including any
    /// password-reset or email-confirmation link — to the logger so a developer
    /// can click through without a live SMTP server.
    /// </summary>
    public class LoggingEmailSender : IEmailSender
    {
        private readonly ILogger<LoggingEmailSender> _logger;

        public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            _logger.LogInformation(
                "[Dev email] To: {Recipient} | Subject: {Subject} | Body: {Body}",
                email, subject, htmlMessage);
            return Task.CompletedTask;
        }
    }
}
