using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services.Billing
{
    /// <summary>
    /// Seeds the four Phase-9 plans (idempotent by PlanCode) and lazily provisions a Stripe
    /// recurring Price for each dealer plan via lookup key. Pay-per-post is one-time so it has
    /// no Stripe Price — its PaymentIntent is created ad-hoc at checkout. Mirrors the
    /// upsert-and-skip style of <see cref="MechanicSeed"/>.
    ///
    /// Note: the project's "regular user" role is <c>Buyer</c> (there is no "User" role), so the
    /// pay-per-post plan targets <c>Buyer</c> — that is what the listing gate matches against.
    /// </summary>
    public static class SubscriptionPlanSeed
    {
        private record PlanDef(string Code, string Name, decimal Jod, PlanBillingInterval Interval, string Role);

        private static readonly PlanDef[] Defs =
        {
            new("DEALER_WEEKLY",   "Weekly",       20m,  PlanBillingInterval.Week,    "Dealer"),
            new("DEALER_MONTHLY",  "Monthly",      60m,  PlanBillingInterval.Month,   "Dealer"),
            new("DEALER_YEARLY",   "Yearly",       500m, PlanBillingInterval.Year,    "Dealer"),
            new("USER_PAYPERPOST", "Pay per post", 2m,   PlanBillingInterval.OneTime, "Buyer"),
        };

        public static async Task SeedAsync(
            CarFitDbContext context, IStripeService stripe, ILogger logger, CancellationToken ct = default)
        {
            var existing = await context.SubscriptionPlans.ToListAsync(ct);

            foreach (var def in Defs)
            {
                var row = existing.FirstOrDefault(p =>
                    string.Equals(p.PlanCode, def.Code, StringComparison.OrdinalIgnoreCase));

                if (row is null)
                {
                    context.SubscriptionPlans.Add(new SubscriptionPlan
                    {
                        PlanCode = def.Code,
                        Name = def.Name,
                        AmountJod = def.Jod,
                        BillingInterval = def.Interval,
                        TargetRole = def.Role,
                        IsActive = true
                    });
                }
                else
                {
                    // Keep editable fields in sync with the canonical definition; preserve StripePriceId.
                    row.Name = def.Name;
                    row.AmountJod = def.Jod;
                    row.BillingInterval = def.Interval;
                    row.TargetRole = def.Role;
                }
            }

            await context.SaveChangesAsync(ct);

            // Provision Stripe Prices for recurring plans (idempotent: EnsureRecurringPriceAsync
            // looks up by PlanCode first). Skipped entirely when Stripe isn't configured.
            if (!stripe.IsConfigured)
            {
                logger.LogWarning("Stripe not configured — subscription plans seeded without Stripe Price ids.");
                return;
            }

            var needPrice = await context.SubscriptionPlans
                .Where(p => p.BillingInterval != PlanBillingInterval.OneTime && p.StripePriceId == null)
                .ToListAsync(ct);

            foreach (var plan in needPrice)
            {
                try
                {
                    plan.StripePriceId = await stripe.EnsureRecurringPriceAsync(plan, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to provision Stripe Price for plan {PlanCode}.", plan.PlanCode);
                }
            }

            await context.SaveChangesAsync(ct);
        }
    }
}
