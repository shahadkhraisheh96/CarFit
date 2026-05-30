using System.ComponentModel.DataAnnotations;
using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    public class CarListingFormViewModel : IValidatableObject
    {
        public int? ListingId { get; set; }
        public int? CarId { get; set; }

        [Required(ErrorMessage = "Make is required.")]
        [StringLength(50)]
        public string Make { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required.")]
        [StringLength(50)]
        public string Model { get; set; } = string.Empty;

        [Required(ErrorMessage = "Year is required.")]
        [Range(1950, 2100, ErrorMessage = "Year must be between 1950 and 2100.")]
        public int Year { get; set; }

        [Required(ErrorMessage = "Please choose New or Used.")]
        [RegularExpression("^(New|Used)$", ErrorMessage = "Type must be New or Used.")]
        public string Type { get; set; } = "Used";

        [StringLength(100)]
        public string? Trim { get; set; }

        [Range(0, 99_999_999)]
        [Display(Name = "Manufacturer price")]
        public decimal? Price { get; set; }

        [StringLength(100)]
        [Display(Name = "Engine size")]
        public string? EngineSize { get; set; }

        [StringLength(20)]
        [Display(Name = "Fuel type")]
        public string? FuelType { get; set; }

        [Required(ErrorMessage = "Transmission is required.")]
        [RegularExpression("^(Automatic|Manual)$", ErrorMessage = "Transmission must be Automatic or Manual.")]
        public string Transmission { get; set; } = "Automatic";

        [StringLength(100)]
        [Display(Name = "Body type")]
        public string? BodyType { get; set; }

        [Range(2, 12, ErrorMessage = "Seats must be between 2 and 12.")]
        public int? Seats { get; set; }

        [StringLength(100)]
        [Display(Name = "Mileage (km)")]
        public string? Kilometers { get; set; }

        [StringLength(100)]
        [Display(Name = "Exterior color")]
        public string? ExteriorColor { get; set; }

        [StringLength(100)]
        [Display(Name = "Interior color")]
        public string? InteriorColor { get; set; }

        [Display(Name = "Interior options")]
        public string? InteriorOptions { get; set; }

        [Display(Name = "Exterior options")]
        public string? ExteriorOptions { get; set; }

        [Display(Name = "Technology options")]
        public string? TechnologyOptions { get; set; }

        // ----- Listing -----
        [Required(ErrorMessage = "Listing price is required.")]
        [Range(0, 99_999_999, ErrorMessage = "Listing price must be a positive amount.")]
        [Display(Name = "Listing price (JD)")]
        public decimal ListingPrice { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment method")]
        public string? PaymentMethodAllowed { get; set; }

        [Display(Name = "Allow installments")]
        public bool InstallmentOption { get; set; }

        /// <summary>
        /// Admin-only override. Dealers always submit as "Pending".
        /// </summary>
        [StringLength(20)]
        public string? Status { get; set; }

        // ----- Images -----
        public List<CarImage> ExistingImages { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Image count rules (FR-3.4) are enforced in the controller, where we
            // know the uploaded file count alongside ExistingImages. Nothing to do
            // here at the moment.
            yield break;
        }
    }
}
