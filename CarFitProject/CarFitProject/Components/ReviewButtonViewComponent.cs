using System.Security.Claims;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Components
{
    /// <summary>
    /// Renders a single "Add Review" / "Edit Review" link whose wording depends
    /// on whether the current user already has a testimonial. Used on the buyer
    /// and seller dashboards and the home page so the lookup lives in one place.
    /// </summary>
    public class ReviewButtonViewComponent : ViewComponent
    {
        private readonly ITestimonialService _testimonials;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewButtonViewComponent(ITestimonialService testimonials, UserManager<ApplicationUser> userManager)
        {
            _testimonials = testimonials;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            string variant = "button",
            string cssClass = "btn btn-outline-primary fw-bold",
            string iconClass = "fa-solid fa-star")
        {
            var hasReview = false;

            if (User is ClaimsPrincipal principal && principal.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(principal);
                if (!string.IsNullOrEmpty(userId))
                {
                    hasReview = await _testimonials.GetByUserAsync(userId) != null;
                }
            }

            return View(new ReviewButtonViewModel
            {
                HasReview = hasReview,
                Variant = variant,
                CssClass = cssClass,
                IconClass = iconClass
            });
        }
    }
}
