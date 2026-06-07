using CarFitProject.Helpers;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace CarFitProject.Services.Billing
{
    /// <summary>Sends transactional SMS for subscription lifecycle events.</summary>
    public interface ISmsService
    {
        /// <summary>True when Twilio credentials are configured. When false, <see cref="SendAsync"/> is a no-op.</summary>
        bool IsConfigured { get; }

        /// <summary>
        /// Send <paramref name="message"/> to <paramref name="rawPhone"/> (any Jordanian format —
        /// it is normalized to E.164). Never throws: delivery failures are logged and swallowed so
        /// they can't break the webhook / billing flow that triggered them.
        /// </summary>
        Task SendAsync(string? rawPhone, string message, CancellationToken ct = default);
    }

    /// <summary>Twilio-backed <see cref="ISmsService"/>; logging no-op when unconfigured.</summary>
    public class TwilioSmsService : ISmsService
    {
        private readonly TwilioSettings _settings;
        private readonly ILogger<TwilioSmsService> _logger;

        public TwilioSmsService(IOptions<TwilioSettings> settings, ILogger<TwilioSmsService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.AccountSid)
            && !_settings.AccountSid!.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_settings.AuthToken)
            && !string.IsNullOrWhiteSpace(_settings.FromNumber);

        public async Task SendAsync(string? rawPhone, string message, CancellationToken ct = default)
        {
            // Reuse Phase-6 normalization (digits, 962 country code), then add the leading '+'.
            var digits = PhoneHelper.ToWaMeNumber(rawPhone);
            if (string.IsNullOrEmpty(digits))
            {
                _logger.LogWarning("SMS skipped: could not normalize phone '{Raw}'.", rawPhone);
                return;
            }
            var to = "+" + digits;

            if (!IsConfigured)
            {
                _logger.LogInformation("SMS (no-op, Twilio not configured) → {To}: {Message}", to, message);
                return;
            }

            try
            {
                TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
                await MessageResource.CreateAsync(
                    to: new PhoneNumber(to),
                    from: new PhoneNumber(_settings.FromNumber),
                    body: message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Twilio SMS to {To} failed.", to);
            }
        }
    }
}
