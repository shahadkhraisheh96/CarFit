using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using CarFitProject.Models;
using CarFitProject.Services;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class DashboardController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly IRecommendationService _recommendations;
        private readonly ISavedCarsService _savedCars;

        public DashboardController(
            CarFitDbContext context,
            IRecommendationService recommendations,
            ISavedCarsService savedCars)
        {
            _context = context;
            _recommendations = recommendations;
            _savedCars = savedCars;
        }

        public async Task<IActionResult> Index(int? activeProfileId)
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var profiles = await _context.UserProfiles
                .Where(p => p.UserId == userId && p.IsActive == true)
                .ToListAsync();

            if (!profiles.Any())
            {
                return RedirectToAction("Start", "Questionnaire");
            }

            var selectedProfile = activeProfileId.HasValue
                ? profiles.FirstOrDefault(p => p.ProfileId == activeProfileId.Value)
                : profiles.First();

            selectedProfile ??= profiles.First();

            ViewBag.AllUserProfiles = profiles;
            ViewBag.CurrentProfileId = selectedProfile.ProfileId;
            ViewBag.CurrentProfileName = selectedProfile.ProfileName;

            var result = await _recommendations.GetMatchesAsync(selectedProfile, userId);
            ViewBag.BudgetRelaxed = result.BudgetRelaxed;
            ViewBag.RelaxationMessage = result.RelaxationMessage;
            ViewBag.SavedCarIds = await _savedCars.GetSavedCarIdsAsync(userId);
            ViewBag.SavedCount = await _savedCars.CountAsync(userId);
            return View(result.Cars);
        }
    }
}
