using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Options;

namespace CarFitProject.Services
{
    /// <summary>
    /// Production <see cref="IEmailSender"/>: sends transactional mail
    /// (password reset, email confirmation) over SMTP using <see cref="EmailSettings"/>.
    /// Throws if Host/From are not configured.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> options, ILogger<SmtpEmailSender> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.From))
            {
                throw new InvalidOperationException(
                    "EmailSettings:Host and EmailSettings:From must be configured before SmtpEmailSender can send.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.From),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrEmpty(_settings.User))
            {
                client.Credentials = new NetworkCredential(_settings.User, _settings.Password);
            }

            await client.SendMailAsync(message);
            _logger.LogInformation("Sent email to {Recipient} via {Host}:{Port}.", email, _settings.Host, _settings.Port);
        }
    }
}
