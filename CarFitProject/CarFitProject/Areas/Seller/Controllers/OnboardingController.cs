using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Dealer")]
    public class OnboardingController : Controller
    {
        private readonly CarFitDbContext _context;

        public OnboardingController(CarFitDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            // Already onboarded? Phone is the simplest signal — the AddCar stopgap
            // doesn't populate it. Send the user to their dashboard instead.
            if (seller != null && !string.IsNullOrWhiteSpace(seller.Phone))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var vm = new DealerOnboardingViewModel
            {
                Name = seller?.Name ?? string.Empty,
                Email = seller?.Email ?? User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                City = seller?.City ?? string.Empty,
                Neighborhood = seller?.Neighborhood ?? string.Empty,
                Phone = seller?.Phone ?? string.Empty
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DealerOnboardingViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            if (seller == null)
            {
                seller = new Models.Seller
                {
                    IdentityUserId = userId,
                    IsApproved = false
                };
                _context.Sellers.Add(seller);
            }

            seller.Name = model.Name.Trim();
            seller.Phone = model.Phone.Trim();
            seller.Email = model.Email.Trim();
            seller.City = model.City.Trim();
            seller.Neighborhood = model.Neighborhood.Trim();
            // IsApproved stays false until an admin approves; Tier stays null.

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your dealership profile has been submitted. An administrator will review your application shortly.";
            return RedirectToAction("Index", "Dashboard");
        }
    }
}
