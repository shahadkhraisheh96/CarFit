namespace CarFitProject.Models.Subscriptions;

/// <summary>
/// A purchasable plan. Dealer plans are recurring Stripe subscriptions (Weekly/Monthly/
/// Yearly); the single User plan is the pay-per-post one-time charge. Amounts are stored
/// and displayed in JOD; Stripe is charged in USD (see Stripe:UsdPerJod) so each recurring
/// plan carries a StripePriceId created lazily by the seeder.
/// </summary>
public class SubscriptionPlan
{
    public int Id { get; set; }

    /// <summary>Stable identifier used as the Stripe Price lookup key. Unique. e.g. "DEALER_WEEKLY".</summary>
    public string PlanCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    /// <summary>Price shown to the user, in JOD. decimal(18,3) for fils precision.</summary>
    public decimal AmountJod { get; set; }

    public PlanBillingInterval BillingInterval { get; set; }

    /// <summary>Identity role this plan targets: "Dealer" or "User".</summary>
    public string TargetRole { get; set; } = null!;

    /// <summary>Stripe Price id for recurring plans. Null for the one-time PayPerPost plan.</summary>
    public string? StripePriceId { get; set; }

    public bool IsActive { get; set; } = true;

    public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
