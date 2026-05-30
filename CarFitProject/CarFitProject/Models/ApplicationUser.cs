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
    }
}
