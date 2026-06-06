using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class TestimonialFormViewModel
    {
        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Your name")]
        public string ReviewerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a star rating.")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }

        [Required(ErrorMessage = "Please write your review.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Review must be between 10 and 1000 characters.")]
        [Display(Name = "Your review")]
        public string ReviewText { get; set; } = string.Empty;

        /// <summary>
        /// True when the caller already has a review and is editing it (drives
        /// the heading / button wording). Not user-editable.
        /// </summary>
        public bool IsEdit { get; set; }
    }
}
