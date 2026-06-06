using CarFitProject.Helpers;
using CarFitProject.Models.Subscriptions;

namespace CarFitProject.ViewModel
{
    /// <summary>One row of the admin subscriptions report.</summary>
    public class AdminSubscriptionRow
    {
        public string UserName { get; set; } = "";
        public string? Email { get; set; }
        public string Role { get; set; } = "";
        public string PlanName { get; set; } = "";
        public decimal FeeJod { get; set; }
        public PlanBillingInterval Interval { get; set; }
        public SubscriptionStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? NextBilling { get; set; }
        public decimal LifetimePaidJod { get; set; }
    }

    /// <summary>Admin subscriptions index: filtered, paginated.</summary>
    public class AdminSubscriptionsViewModel
    {
        public PaginatedList<AdminSubscriptionRow> Rows { get; set; } = new(new(), 0, 1, 20);
        public string? StatusFilter { get; set; }
    }

    /// <summary>Plan-picker page. Plans are pre-filtered to the current user's role.</summary>
    public class PlanSelectionViewModel
    {
        public string Role { get; set; } = "Buyer";
        public bool IsDealer => Role == "Dealer";
        public List<SubscriptionPlan> Plans { get; set; } = new();

        /// <summary>Current/most-recent subscription, if any (drives "you're already subscribed" messaging).</summary>
        public Subscription? Current { get; set; }

        /// <summary>Buyer-only: whether the free trial has been started already.</summary>
        public bool TrialStarted { get; set; }
        public DateTime? TrialEndsAt { get; set; }
        public bool TrialActive => TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;

        /// <summary>Pay-per-post amount in JOD (for the Buyer call-to-action).</summary>
        public decimal PerPostJod { get; set; }
    }

    /// <summary>Billing dashboard: current plan, history, manage actions.</summary>
    public class ManageBillingViewModel
    {
        public Subscription? Current { get; set; }
        public List<PaymentTransaction> History { get; set; } = new();
        public decimal LifetimePaidJod { get; set; }

        /// <summary>True when the user has a Stripe customer (so the billing portal / card update is usable).</summary>
        public bool HasStripeCustomer { get; set; }

        /// <summary>Buyer-only: unconsumed pay-per-post credits available for the next listing.</summary>
        public int PerPostCredits { get; set; }

        public bool IsDealer { get; set; }
        public DateTime? TrialEndsAt { get; set; }
    }
}
