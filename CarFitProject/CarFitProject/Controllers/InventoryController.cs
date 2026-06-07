using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.Services.Billing;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class InventoryController : Controller
    {
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;
        private readonly IInspectionScoringService _scoring;
        private readonly ISavedCarsService _savedCars;
        private readonly ISubscriptionService _subscriptions;
        private readonly ISellerSubscriptionService _sellerSubs;

        public InventoryController(
            CarFitDbContext context,
            IInspectionScoringService scoring,
            ISavedCarsService savedCars,
            ISubscriptionService subscriptions,
            ISellerSubscriptionService sellerSubs)
        {
            _context = context;
            _scoring = scoring;
            _savedCars = savedCars;
            _subscriptions = subscriptions;
            _sellerSubs = sellerSubs;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: /Inventory/Search?make=Toyota&priceTo=15000&page=2
        [HttpGet]
        public async Task<IActionResult> Search(ListingSearchViewModel filters)
        {
            // Only approved listings are publicly searchable (Phase 10 approval flow).
            var query = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Where(l => l.Status == ListingStatus.Approved);

            // Filter Matrix Bindings
            if (!string.IsNullOrWhiteSpace(filters.Make))
                query = query.Where(l => l.Car!.Make.Contains(filters.Make));
            if (!string.IsNullOrWhiteSpace(filters.Model))
                query = query.Where(l => l.Car!.Model.Contains(filters.Model));
            if (filters.YearFrom.HasValue)
                query = query.Where(l => l.Car!.Year >= filters.YearFrom.Value);
            if (filters.YearTo.HasValue)
                query = query.Where(l => l.Car!.Year <= filters.YearTo.Value);
            if (filters.PriceFrom.HasValue)
                query = query.Where(l => l.ListingPrice >= filters.PriceFrom.Value);
            if (filters.PriceTo.HasValue)
                query = query.Where(l => l.ListingPrice <= filters.PriceTo.Value);
            if (!string.IsNullOrWhiteSpace(filters.Type))
                query = query.Where(l => l.Car!.Type == filters.Type);
            if (!string.IsNullOrWhiteSpace(filters.Transmission))
                query = query.Where(l => l.Car!.Transmission == filters.Transmission);
            if (filters.SellerId.HasValue)
                query = query.Where(l => l.SellerId == filters.SellerId.Value);

            // Priority placement (Phase 9): listings from active subscribers float to the top,
            // then fall back to the existing newest-first ordering.
            var prioritizedSellerIds = await _sellerSubs.GetActiveSubscriberSellerIdsAsync();
            var ordered = query
                .OrderByDescending(l => l.SellerId != null && prioritizedSellerIds.Contains(l.SellerId.Value))
                .ThenByDescending(l => l.Id);

            filters.Results = await PaginatedList<CarListing>.CreateAsync(
                ordered, filters.Page, PageSize);

            await LogSearchAsync(filters);

            ViewBag.SavedCarIds = User.IsInRole("Buyer")
                ? await _savedCars.GetSavedCarIdsAsync(CurrentUserId!)
                : new HashSet<int>();
            return View(filters);
        }

        // GET: /Inventory/Detail/42
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            // Detail pages are public only for approved listings (Phase 10 approval flow).
            var listing = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id && l.Status == ListingStatus.Approved);

            if (listing == null) return NotFound();

            if (listing.Car?.InspectionReport != null)
            {
                ViewBag.InspectionSignals = _scoring.Compute(listing.Car.InspectionReport);
            }

            ViewBag.IsBuyer = User.IsInRole("Buyer");
            if (ViewBag.IsBuyer)
            {
                ViewBag.IsSaved = await _context.SavedResults
                    .AnyAsync(s => s.UserId == CurrentUserId && s.CarId == listing.CarId);
                ViewBag.IsPremium = await _subscriptions.IsPremiumAsync(CurrentUserId);
            }

            var sellerCity = listing.Seller?.City;
            ViewBag.Mechanics = await _context.Mechanics
                .AsNoTracking()
                .OrderBy(m => m.City).ThenBy(m => m.Name)
                .ToListAsync();
            ViewBag.SellerCity = sellerCity;
            return View(listing);
        }

        // POST: /Inventory/RecordContactIntent/{id} — logs a buyer lead when a logged-in user
        // clicks "Buy This Car". One row per click (not deduplicated). Best-effort: returns 200.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordContactIntent(int id)
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var listing = await _context.CarListings
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (listing == null) return NotFound();

            _context.BuyerContactIntents.Add(new BuyerContactIntent
            {
                ListingId = id,
                BuyerUserId = userId,
                SellerUserId = listing.Seller?.IdentityUserId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return Ok();
        }

        private async Task LogSearchAsync(ListingSearchViewModel filters)
        {
            if (filters.Page > 1) return;

            bool hasFilter =
                !string.IsNullOrWhiteSpace(filters.Make) ||
                !string.IsNullOrWhiteSpace(filters.Model) ||
                filters.YearFrom.HasValue ||
                filters.YearTo.HasValue ||
                filters.PriceFrom.HasValue ||
                filters.PriceTo.HasValue ||
                !string.IsNullOrWhiteSpace(filters.Type) ||
                !string.IsNullOrWhiteSpace(filters.Transmission);
            if (!hasFilter) return;

            var term = string.Join(" ",
                new[] { filters.Make, filters.Model }
                    .Where(s => !string.IsNullOrWhiteSpace(s)))
                .Trim();
            if (term.Length > 255) term = term[..255];

            var snapshot = new
            {
                filters.Make,
                filters.Model,
                filters.YearFrom,
                filters.YearTo,
                filters.PriceFrom,
                filters.PriceTo,
                filters.Type,
                filters.Transmission
            };

            _context.SearchLogs.Add(new SearchLog
            {
                Term = string.IsNullOrEmpty(term) ? null : term,
                FiltersJson = System.Text.Json.JsonSerializer.Serialize(snapshot),
                UserId = CurrentUserId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        // GET: /Inventory/GetTermExplanation?term=...
        [HttpGet]
        public async Task<IActionResult> GetTermExplanation(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return BadRequest();

            var match = await _context.InspectionTermsGlossaries
                .FirstOrDefaultAsync(g => g.Term.Trim().ToLower() == term.Trim().ToLower());

            if (match == null) return NotFound();

            return Json(new
            {
                severity = match.SeverityLevel,
                ar = match.ExplanationAr,
                en = match.ExplanationEn
            });
        }
    }
}