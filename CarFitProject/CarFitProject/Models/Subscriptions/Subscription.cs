namespace CarFitProject.Models.Subscriptions;

/// <summary>
/// A seller's subscription record. UserId is the AspNetUsers.Id (string, 450) — the project's
/// established cross-context link pattern (see Seller.IdentityUserId, SavedResult.UserId); there
/// is intentionally no EF navigation to ApplicationUser, which lives in ApplicationDbContext.
/// </summary>
public class Subscription
{
    public int Id { get; set; }

    /// <summary>AspNetUsers.Id of the subscribing seller.</summary>
    public string UserId { get; set; } = null!;

    public int PlanId { get; set; }

    /// <summary>Stripe subscription id. Null during a User free trial and for pay-per-post-only users.</summary>
    public string? StripeSubscriptionId { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? TrialEndsAt { get; set; }

    public DateTime? CurrentPeriodStart { get; set; }

    public DateTime? CurrentPeriodEnd { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual SubscriptionPlan Plan { get; set; } = null!;

    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}
