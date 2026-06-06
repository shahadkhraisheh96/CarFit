using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Controllers
{
    // Website testimonials are left by signed-in buyers and dealers. (The role
    // formerly called "Seller" is now "Dealer" — see SeedIdentityAsync.)
    [Authorize(Roles = "Buyer,Dealer")]
    public class TestimonialsController : Controller
    {
        private readonly ITestimonialService _testimonials;
        private readonly UserManager<ApplicationUser> _userManager;

        public TestimonialsController(ITestimonialService testimonials, UserManager<ApplicationUser> userManager)
        {
            _testimonials = testimonials;
            _userManager = userManager;
        }

        // Single entry point for both creating and editing — a user has at most
        // one review, so the form loads their existing one when present.
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var existing = user == null ? null : await _testimonials.GetByUserAsync(user.Id);

            var vm = existing == null
                ? new TestimonialFormViewModel
                {
                    ReviewerName = user?.FullName ?? user?.UserName ?? string.Empty,
                    IsEdit = false
                }
                : new TestimonialFormViewModel
                {
                    ReviewerName = existing.ReviewerName,
                    Rating = existing.Rating,
                    ReviewText = existing.ReviewText,
                    IsEdit = true
                };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TestimonialFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User)!;
            var alreadyReviewed = await _testimonials.GetByUserAsync(userId) != null;

            // Upsert: creates the review or updates the existing one, resetting it
            // to pending either way.
            await _testimonials.SaveAsync(userId, model.ReviewerName, model.Rating, model.ReviewText);

            TempData["SuccessMessage"] = alreadyReviewed
                ? "Your review has been updated and is pending admin approval again before it shows on the home page."
                : "Thanks for your review! It will appear on the home page once an admin approves it.";
            return RedirectToAction(nameof(Create));
        }
    }
}
