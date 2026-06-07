using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class ProfilesController : Controller
    {
        private readonly CarFitDbContext _context;

        public ProfilesController(CarFitDbContext context)
        {
            _context = context;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var profiles = await _context.UserProfiles
                .Where(p => p.UserId == UserId)
                .OrderByDescending(p => p.IsActive == true)
                .ThenBy(p => p.ProfileId)
                .ToListAsync();

            return View(profiles);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var owned = await _context.UserProfiles
                .Where(p => p.UserId == UserId)
                .ToListAsync();

            var target = owned.FirstOrDefault(p => p.ProfileId == id);
            if (target == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var p in owned)
            {
                p.IsActive = p.ProfileId == id;
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Set \"{target.ProfileName}\" as your active profile.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Rename(int id, string profileName)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            if (string.IsNullOrWhiteSpace(profileName))
            {
                TempData["ErrorMessage"] = "Profile name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.ProfileId == id && p.UserId == UserId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            profile.ProfileName = profileName.Trim();
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Renamed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.ProfileId == id && p.UserId == UserId);

            if (profile == null)
            {
                TempData["ErrorMessage"] = "Profile not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.UserProfiles.Remove(profile);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted profile \"{profile.ProfileName}\".";
            return RedirectToAction(nameof(Index));
        }
    }
}
