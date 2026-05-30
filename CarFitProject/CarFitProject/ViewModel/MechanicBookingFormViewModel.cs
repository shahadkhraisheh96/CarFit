using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class MechanicBookingFormViewModel
    {
        [Required]
        public int CarListingId { get; set; }

        [Display(Name = "Mechanic")]
        public int MechanicId { get; set; }

        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100)]
        [Display(Name = "Your name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email.")]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Package")]
        public string? PackageType { get; set; }

        [Required(ErrorMessage = "Please pick a preferred date.")]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred date")]
        public DateTime PreferredDate { get; set; } = DateTime.Today.AddDays(2);

        [StringLength(500)]
        [Display(Name = "Vehicle notes")]
        public string? VehicleNotes { get; set; }
    }
}
