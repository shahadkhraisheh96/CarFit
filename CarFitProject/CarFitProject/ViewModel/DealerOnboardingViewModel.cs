using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class DealerOnboardingViewModel
    {
        [Required(ErrorMessage = "Please enter your dealership name.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters.")]
        [Display(Name = "Dealership name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a contact phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20)]
        [Display(Name = "Phone")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a contact email.")]
        [EmailAddress]
        [StringLength(255)]
        [Display(Name = "Contact email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your city.")]
        [StringLength(100)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your neighborhood.")]
        [StringLength(100)]
        [Display(Name = "Neighborhood")]
        public string Neighborhood { get; set; } = string.Empty;
    }
}
