namespace CarFitProject.Services.Billing
{
    /// <summary>
    /// Stripe configuration. Keys are bound from user-secrets (dev) / environment (prod) —
    /// never appsettings.json. <see cref="UsdPerJod"/> is the fixed conversion rate used to
    /// charge Stripe in USD while every amount shown to the user stays in JOD.
    /// </summary>
    public class StripeSettings
    {
        public string? SecretKey { get; set; }

        public string? PublishableKey { get; set; }

        public string? WebhookSecret { get; set; }

        /// <summary>USD per 1 JOD. JOD prices are multiplied by this before charging Stripe.</summary>
        public decimal UsdPerJod { get; set; } = 1.41m;
    }
}
