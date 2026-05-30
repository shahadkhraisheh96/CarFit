using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
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

        public InventoryController(
            CarFitDbContext context,
            IInspectionScoringService scoring,
            ISavedCarsService savedCars,
            ISubscriptionService subscriptions)
        {
            _context = context;
            _scoring = scoring;
            _savedCars = savedCars;
            _subscriptions = subscriptions;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // GET: /Inventory/Search?make=Toyota&priceTo=15000&page=2
        [HttpGet]
        public async Task<IActionResult> Search(ListingSearchViewModel filters)
        {
            // 🛠️ MULTI-STATUS FIX: Accept both "Active" and "Available" states dynamically
            var query = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Where(l => l.Status == "Active" || l.Status == "Available");

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

            filters.Results = await PaginatedList<CarListing>.CreateAsync(
                query.OrderByDescending(l => l.Id), filters.Page, PageSize);

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
            // 🛠️ MULTI-STATUS FIX: Ensure detail pages load regardless of which status variation is used
            var listing = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id && (l.Status == "Active" || l.Status == "Available"));

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