using System.ComponentModel.DataAnnotations;

namespace CarFitProject.Models
{
    /// <summary>
    /// A lead signal: one row each time a logged-in buyer clicks "Buy This Car" on a listing
    /// (not deduplicated — repeat clicks = repeat interest). UserId columns follow the project's
    /// plain-string Identity-link convention (no cross-context FK); ListingId is a plain int so the
    /// table stays lightweight and a listing deletion never blocks on leads.
    /// </summary>
    public class BuyerContactIntent
    {
        public int Id { get; set; }

        public int ListingId { get; set; }

        [MaxLength(450)]
        public string BuyerUserId { get; set; } = null!;

        [MaxLength(450)]
        public string? SellerUserId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
