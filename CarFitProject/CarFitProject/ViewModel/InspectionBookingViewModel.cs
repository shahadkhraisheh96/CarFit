using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class InspectionBookingViewModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string CustomerEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an inspection package option.")]
        public string PackageType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify a preferred inspection date.")]
        [DataType(DataType.Date)]
        public DateTime PreferredDate { get; set; }

        [StringLength(500, ErrorMessage = "Vehicle details cannot exceed 500 characters.")]
        public string? VehicleNotes { get; set; }
    }
}
