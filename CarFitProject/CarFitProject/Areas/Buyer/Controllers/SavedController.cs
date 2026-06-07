using CarFitProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarFitProject.Areas.Buyer.Controllers
{
    /// <summary>
    /// Buyer's "Saved cars" bookmarks (FR-6.1). Index renders the paged set;
    /// Toggle is the single mutation endpoint (Save when missing / Unsave when
    /// present) and honours an optional local returnUrl so the buyer lands back
    /// on the page they came from (Dashboard, Search, Detail, etc.).
    /// </summary>
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class SavedController : Controller
    {
        private const int PageSize = 12;

        private readonly ISavedCarsService _saved;
        private readonly ISubscriptionService _subscriptions;

        public SavedController(ISavedCarsService saved, ISubscriptionService subscriptions)
        {
            _saved = saved;
            _subscriptions = subscriptions;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        /// <summary>Paged Saved Cars view (12/page — NFR-Sc1).</summary>
        public async Task<IActionResult> Index(int page = 1)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var listings = await _saved.GetSavedListingsPagedAsync(UserId, page, PageSize);
            ViewBag.IsPremium = await _subscriptions.IsPremiumAsync(UserId);
            ViewBag.FreeLimit = _subscriptions.FreeSaveLimit;
            ViewBag.SavedCount = await _saved.CountAsync(UserId);
            return View(listings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int carId, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var result = await _saved.ToggleAsync(UserId, carId);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
