using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CarFitProject.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Buyer subscription plan that gates Save-Car capacity (3 free / unlimited
        /// premium) and the email-contact button. Values: "Free" / "Premium".
        /// Distinct from Seller.Tier, which is the dealer subscription concept.
        /// </summary>
        public string SubscriptionTier { get; set; } = "Free";

        // ── Phase 9: Stripe billing (seller/user listing subscriptions) ──
        // The Subscription/PaymentTransaction tables live in CarFitDbContext and link
        // back here by Id (string). These three columns live on AspNetUsers.

        /// <summary>Stripe Customer id for this user. Null until first checkout.</summary>
        [MaxLength(255)]
        public string? StripeCustomerId { get; set; }

        /// <summary>Stripe PaymentMethod id used as the customer's default. Null until set.</summary>
        [MaxLength(255)]
        public string? DefaultPaymentMethodId { get; set; }

        /// <summary>When the User-role free trial first started. Null for users who never began a trial.</summary>
        public DateTime? TrialStartedAt { get; set; }

        /// <summary>
        /// True when PhoneNumber is a system-assigned placeholder (from the backfill), not a real
        /// number the user entered. Placeholder users are nudged to set a real phone on next login.
        /// </summary>
        public bool PhoneIsPlaceholder { get; set; }
    }
}
