using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Dealer")]
    public class DashboardController : Controller
    {
        private readonly CarFitDbContext _context;

        public DashboardController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var seller = await _context.Sellers
                .FirstOrDefaultAsync(s => s.IdentityUserId == userId);

            // No Seller row, or only the AddCar stopgap (no phone): force onboarding.
            if (seller == null || string.IsNullOrWhiteSpace(seller.Phone))
            {
                return RedirectToAction("Index", "Onboarding");
            }

            return View(seller);
        }
    }
}
