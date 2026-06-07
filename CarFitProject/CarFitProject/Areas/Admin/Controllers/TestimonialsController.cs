using CarFitProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class TestimonialsController : Controller
    {
        private readonly ITestimonialService _testimonials;

        public TestimonialsController(ITestimonialService testimonials)
        {
            _testimonials = testimonials;
        }

        // Full management view: every testimonial, pending and approved.
        public async Task<IActionResult> Index()
        {
            var all = await _testimonials.GetAllAsync();
            return View(all);
        }

        public async Task<IActionResult> Details(int id)
        {
            var testimonial = await _testimonials.GetByIdAsync(id);
            if (testimonial == null)
            {
                TempData["ErrorMessage"] = "That review no longer exists.";
                return RedirectToAction(nameof(Index));
            }
            return View(testimonial);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var ok = await _testimonials.ApproveAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Review approved — it's now live on the home page."
                : "That review no longer exists.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await _testimonials.DeleteAsync(id);
            TempData[ok ? "SuccessMessage" : "ErrorMessage"] = ok
                ? "Review deleted."
                : "That review no longer exists.";
            return RedirectToAction(nameof(Index));
        }
    }
}
