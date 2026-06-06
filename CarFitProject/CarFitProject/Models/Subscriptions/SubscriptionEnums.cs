namespace CarFitProject.Models.Subscriptions;

/// <summary>Lifecycle of a seller subscription, mirrored from Stripe webhook events.</summary>
public enum SubscriptionStatus
{
    Trial,
    Active,
    PastDue,
    Cancelled,
    Expired
}

/// <summary>Billing cadence of a plan. OneTime is the User pay-per-post charge (not recurring).</summary>
public enum PlanBillingInterval
{
    Week,
    Month,
    Year,
    OneTime
}

/// <summary>Whether a PaymentTransaction is a recurring subscription charge or a one-off per-post fee.</summary>
public enum TransactionType
{
    Subscription,
    PerPost
}
