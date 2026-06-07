using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace CarFitProject.Areas.Admin.Controllers
{
    /// <summary>Admin maintenance actions. Currently: one-off backfill of placeholder phone numbers.</summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ToolsController : Controller
    {
        private static readonly string[] Prefixes = { "077", "078", "079" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ToolsController(UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.NullPhoneCount = await _userManager.Users
                .CountAsync(u => u.PhoneNumber == null || u.PhoneNumber == "");
            return View();
        }

        // POST /Admin/Tools/BackfillPhones — assigns a unique random Jordanian mobile (placeholder)
        // to every user with no phone. Idempotent: re-running only touches users still missing a phone.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackfillPhones()
        {
            var users = await _userManager.Users.ToListAsync();
            var existing = new HashSet<string>(
                users.Where(u => !string.IsNullOrEmpty(u.PhoneNumber)).Select(u => u.PhoneNumber!),
                StringComparer.OrdinalIgnoreCase);

            int already = existing.Count;
            int updated = 0;

            foreach (var u in users.Where(u => string.IsNullOrEmpty(u.PhoneNumber)))
            {
                string? normalized = null;
                for (int attempt = 0; attempt < 50; attempt++) // safety cap against an unlikely collision storm
                {
                    var local = Prefixes[Random.Shared.Next(Prefixes.Length)]
                                + Random.Shared.Next(0, 10_000_000).ToString("D7");
                    var candidate = "+" + PhoneHelper.ToWaMeNumber(local); // E.164, e.g. +962791234567
                    if (!existing.Contains(candidate)) { normalized = candidate; break; }
                }
                if (normalized == null) continue;

                existing.Add(normalized);
                u.PhoneNumber = normalized;
                u.PhoneIsPlaceholder = true;
                await _userManager.UpdateAsync(u);
                updated++;
            }

            TempData["SuccessMessage"] = string.Format(
                _localizer["Updated {0} users. {1} already had phones."].Value, updated, already);
            return RedirectToAction(nameof(Index));
        }
    }
}
