using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        [Display(Name = "Your Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Message is required")]
        public string Message { get; set; } = string.Empty;
    }
}
