using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace CarFitProject.Services.Billing
{
    /// <summary>
    /// Thin wrapper over Stripe.net for the seller-subscription flows. All amounts come in as JOD
    /// and are converted to USD cents via <see cref="StripeSettings.UsdPerJod"/> before they reach
    /// Stripe — the platform charges USD while the UI always shows JOD.
    /// </summary>
    public interface IStripeService
    {
        /// <summary>True when a Stripe secret key is configured.</summary>
        bool IsConfigured { get; }

        /// <summary>Publishable key for client-side use (safe to expose).</summary>
        string? PublishableKey { get; }

        /// <summary>JOD → USD minor units (cents), using the configured fixed rate.</summary>
        long ToUsdCents(decimal amountJod);

        /// <summary>Ensure <paramref name="user"/> has a Stripe Customer; persists the id if newly created.</summary>
        Task<string> EnsureCustomerAsync(ApplicationUser user, CancellationToken ct = default);

        /// <summary>Idempotently create (by lookup key = PlanCode) a recurring Price for a plan; returns its id.</summary>
        Task<string> EnsureRecurringPriceAsync(SubscriptionPlan plan, CancellationToken ct = default);

        /// <summary>Create a subscription-mode Checkout Session for a recurring (dealer) plan; returns the redirect URL.</summary>
        Task<string> CreateSubscriptionCheckoutAsync(
            ApplicationUser user, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default);

        /// <summary>Create a one-time payment-mode Checkout Session for the per-post fee (a standalone PaymentIntent); returns the redirect URL.</summary>
        Task<string> CreatePerPostCheckoutAsync(
            ApplicationUser user, SubscriptionPlan plan, int? carListingId, string successUrl, string cancelUrl, CancellationToken ct = default);

        /// <summary>Create a Stripe Billing Portal session so the customer can manage their card; returns the redirect URL.</summary>
        Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken ct = default);

        /// <summary>Schedule cancellation of a subscription at the end of the current period (not immediately).</summary>
        Task CancelAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken ct = default);

        /// <summary>Verify the Stripe-Signature header and deserialize the webhook payload. Throws on bad signature.</summary>
        Event ConstructWebhookEvent(string json, string signatureHeader);
    }

    /// <inheritdoc cref="IStripeService"/>
    public class StripeService : IStripeService
    {
        private readonly StripeSettings _settings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStripeClient? _client;

        public StripeService(IOptions<StripeSettings> settings, UserManager<ApplicationUser> userManager)
        {
            _settings = settings.Value;
            _userManager = userManager;
            if (IsConfigured)
            {
                _client = new StripeClient(_settings.SecretKey);
            }
        }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(_settings.SecretKey)
            && _settings.SecretKey!.StartsWith("sk_", StringComparison.Ordinal);

        public string? PublishableKey => _settings.PublishableKey;

        public long ToUsdCents(decimal amountJod)
            => (long)Math.Round(amountJod * _settings.UsdPerJod * 100m, MidpointRounding.AwayFromZero);

        private IStripeClient Client =>
            _client ?? throw new InvalidOperationException("Stripe is not configured (missing Stripe:SecretKey).");

        public async Task<string> EnsureCustomerAsync(ApplicationUser user, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(user.StripeCustomerId)) return user.StripeCustomerId;

            var service = new CustomerService(Client);
            var customer = await service.CreateAsync(new CustomerCreateOptions
            {
                Email = user.Email,
                Name = user.FullName,
                Metadata = new Dictionary<string, string> { ["appUserId"] = user.Id }
            }, cancellationToken: ct);

            user.StripeCustomerId = customer.Id;
            await _userManager.UpdateAsync(user);
            return customer.Id;
        }

        public async Task<string> EnsureRecurringPriceAsync(SubscriptionPlan plan, CancellationToken ct = default)
        {
            if (plan.BillingInterval == PlanBillingInterval.OneTime)
                throw new InvalidOperationException($"Plan {plan.PlanCode} is one-time; it has no recurring Price.");

            var service = new PriceService(Client);

            // Look up by stable lookup key so re-seeding is idempotent across restarts.
            var existing = await service.ListAsync(new PriceListOptions
            {
                LookupKeys = new List<string> { plan.PlanCode },
                Active = true,
                Limit = 1
            }, cancellationToken: ct);
            if (existing.Data.Count > 0) return existing.Data[0].Id;

            var price = await service.CreateAsync(new PriceCreateOptions
            {
                Currency = "usd",
                UnitAmount = ToUsdCents(plan.AmountJod),
                LookupKey = plan.PlanCode,
                Recurring = new PriceRecurringOptions { Interval = MapInterval(plan.BillingInterval) },
                ProductData = new PriceProductDataOptions { Name = plan.Name }
            }, cancellationToken: ct);

            return price.Id;
        }

        public async Task<string> CreateSubscriptionCheckoutAsync(
            ApplicationUser user, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default)
        {
            var customerId = await EnsureCustomerAsync(user, ct);
            var priceId = !string.IsNullOrEmpty(plan.StripePriceId)
                ? plan.StripePriceId
                : await EnsureRecurringPriceAsync(plan, ct);

            var metadata = new Dictionary<string, string>
            {
                ["appUserId"] = user.Id,
                ["planCode"] = plan.PlanCode,
                ["type"] = nameof(TransactionType.Subscription)
            };

            var service = new SessionService(Client);
            var session = await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = customerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new() { Price = priceId, Quantity = 1 }
                },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata,
                // Copy the metadata onto the Subscription itself so customer.subscription.*
                // webhook events can resolve the app user and plan without a session lookup.
                SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata }
            }, cancellationToken: ct);

            return session.Url;
        }

        public async Task<string> CreatePerPostCheckoutAsync(
            ApplicationUser user, SubscriptionPlan plan, int? carListingId, string successUrl, string cancelUrl, CancellationToken ct = default)
        {
            var customerId = await EnsureCustomerAsync(user, ct);

            var metadata = new Dictionary<string, string>
            {
                ["appUserId"] = user.Id,
                ["planCode"] = plan.PlanCode,
                ["type"] = nameof(TransactionType.PerPost)
            };
            if (carListingId.HasValue) metadata["carListingId"] = carListingId.Value.ToString();

            var service = new SessionService(Client);
            var session = await service.CreateAsync(new SessionCreateOptions
            {
                Mode = "payment",
                Customer = customerId,
                LineItems = new List<SessionLineItemOptions>
                {
                    new()
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = ToUsdCents(plan.AmountJod),
                            ProductData = new SessionLineItemPriceDataProductDataOptions { Name = plan.Name }
                        }
                    }
                },
                // Surface our metadata on the resulting PaymentIntent so the webhook can reconcile it.
                PaymentIntentData = new SessionPaymentIntentDataOptions { Metadata = metadata },
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                Metadata = metadata
            }, cancellationToken: ct);

            return session.Url;
        }

        public async Task<string> CreateBillingPortalAsync(string customerId, string returnUrl, CancellationToken ct = default)
        {
            var service = new Stripe.BillingPortal.SessionService(Client);
            var session = await service.CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = returnUrl
            }, cancellationToken: ct);

            return session.Url;
        }

        public async Task CancelAtPeriodEndAsync(string stripeSubscriptionId, CancellationToken ct = default)
        {
            var service = new Stripe.SubscriptionService(Client);
            await service.UpdateAsync(stripeSubscriptionId,
                new SubscriptionUpdateOptions { CancelAtPeriodEnd = true },
                cancellationToken: ct);
        }

        public Event ConstructWebhookEvent(string json, string signatureHeader)
        {
            if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
                throw new InvalidOperationException("Stripe:WebhookSecret is not configured.");
            return EventUtility.ConstructEvent(json, signatureHeader, _settings.WebhookSecret);
        }

        private static string MapInterval(PlanBillingInterval interval) => interval switch
        {
            PlanBillingInterval.Week => "week",
            PlanBillingInterval.Month => "month",
            PlanBillingInterval.Year => "year",
            _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Not a recurring interval.")
        };
    }
}
