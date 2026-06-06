namespace CarFitProject.Models.Subscriptions;

/// <summary>
/// A single Stripe money movement: a recurring invoice charge (Type=Subscription) or a one-off
/// pay-per-post fee (Type=PerPost). StripePaymentIntentId carries a UNIQUE filtered index so the
/// webhook handler is idempotent — re-delivered events that reference an already-recorded charge
/// are no-ops. AmountJod is the JOD amount shown to the user (Stripe itself charges USD).
/// </summary>
public class PaymentTransaction
{
    public int Id { get; set; }

    /// <summary>Owning subscription. Null for standalone pay-per-post charges.</summary>
    public int? SubscriptionId { get; set; }

    /// <summary>AspNetUsers.Id of the payer.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Stripe PaymentIntent (or invoice/charge) id. Idempotency key — unique when present.</summary>
    public string? StripePaymentIntentId { get; set; }

    /// <summary>Amount shown to the user, in JOD. decimal(18,3).</summary>
    public decimal AmountJod { get; set; }

    /// <summary>"Succeeded" | "Failed" | "Pending".</summary>
    public string Status { get; set; } = null!;

    public TransactionType Type { get; set; }

    /// <summary>The listing this per-post fee unlocked. Null for subscription charges.</summary>
    public int? CarListingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Subscription? Subscription { get; set; }

    public virtual CarListing? CarListing { get; set; }
}
