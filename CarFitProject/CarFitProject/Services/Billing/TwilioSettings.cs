namespace CarFitProject.Services.Billing
{
    /// <summary>
    /// Twilio SMS configuration, bound from user-secrets / environment. When any field is
    /// missing the SMS service degrades to a logging no-op instead of throwing.
    /// </summary>
    public class TwilioSettings
    {
        public string? AccountSid { get; set; }

        public string? AuthToken { get; set; }

        /// <summary>Sender number in E.164 form, e.g. "+12025550123".</summary>
        public string? FromNumber { get; set; }
    }
}
