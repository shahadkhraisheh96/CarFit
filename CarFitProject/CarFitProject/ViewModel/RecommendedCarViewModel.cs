using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.ViewModel
{
    [Keyless]
    public class RecommendedCarViewModel
    {
        [Column("car_id")]
        public int CarId { get; set; }

        [Column("make")]
        public string Make { get; set; } = null!;

        [Column("model")]
        public string Model { get; set; } = null!;

        [Column("year")]
        public int Year { get; set; }

        [Column("listing_price")]
        public decimal ListingPrice { get; set; }

        [Column("city")]
        public string? City { get; set; }

        [Column("body_condition")]
        public string? BodyCondition { get; set; }

        [Column("description_score")]
        public string? DescriptionScore { get; set; }

        [Column("trust_score")]
        public decimal TrustScore { get; set; }

        [Column("DynamicMatchScore")]
        public int DynamicMatchScore { get; set; }

        // ---- In-memory only ----
        [NotMapped]
        public int? ListingId { get; set; }

        [NotMapped]
        public bool IsRisky { get; set; }

        [NotMapped]
        public string? PrimaryImageUrl { get; set; }

        [NotMapped]
        public bool CarseerAttached { get; set; }

        [NotMapped]
        public List<string> MatchReasons { get; set; } = new();
    }
}
