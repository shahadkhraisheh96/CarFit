using System.ComponentModel.DataAnnotations;

namespace CarFitProject.Models
{
    public class InspectionBooking
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string PackageType { get; set; } = string.Empty;

        [Required]
        public DateTime PreferredDate { get; set; }

        [StringLength(500)]
        public string? VehicleNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Optional link to the listing the buyer is shortlisting (FR-6.4).
        public int? CarListingId { get; set; }
        public virtual CarListing? CarListing { get; set; }

        // Optional pre-selected mechanic (FR-6.4); booking is still valid without one.
        public int? MechanicId { get; set; }
        public virtual Mechanic? Mechanic { get; set; }
    }
}
