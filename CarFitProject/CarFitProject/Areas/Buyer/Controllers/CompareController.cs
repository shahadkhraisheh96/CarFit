using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class CompareController : Controller
    {
        private const int MinCars = 2;
        private const int MaxCars = 3;

        private readonly CarFitDbContext _context;
        private readonly IInspectionScoringService _scoring;

        public CompareController(CarFitDbContext context, IInspectionScoringService scoring)
        {
            _context = context;
            _scoring = scoring;
        }

        // GET /Buyer/Compare?ids=1,2,3
        public async Task<IActionResult> Index(string? ids)
        {
            var listingIds = ParseIds(ids).Take(MaxCars).ToList();
            if (listingIds.Count < MinCars)
            {
                TempData["ErrorMessage"] = $"Pick {MinCars}–{MaxCars} cars to compare.";
                return Redirect("/Inventory/Search");
            }

            var listings = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .Where(l => listingIds.Contains(l.Id))
                .ToListAsync();

            // Preserve the user-picked order.
            var ordered = listingIds
                .Select(id => listings.FirstOrDefault(l => l.Id == id))
                .Where(l => l != null)
                .Cast<CarListing>()
                .ToList();

            var vm = new CompareViewModel
            {
                Cars = ordered.Select(l =>
                {
                    var row = new CompareCarRow
                    {
                        Listing = l,
                        PrimaryImageUrl = l.Car?.CarImages?
                            .OrderByDescending(i => i.IsPrimary)
                            .ThenBy(i => i.SortOrder)
                            .FirstOrDefault()?.Url
                    };
                    if (l.Car?.InspectionReport != null)
                    {
                        var signals = _scoring.Compute(l.Car.InspectionReport);
                        row.InspectionOverall = signals.OverallScore;
                        row.InspectionTrust = signals.CalculatedTrustScore;
                        row.IsRisky = signals.IsRisky;
                        row.EngineStatus = signals.Engine.ToString();
                        row.GearboxStatus = signals.Gearbox.ToString();
                    }
                    else if (string.Equals(l.Car?.Type, "New", StringComparison.OrdinalIgnoreCase))
                    {
                        row.InspectionOverall = 9.99m;
                        row.InspectionTrust = 5m;
                        row.EngineStatus = "Good";
                        row.GearboxStatus = "Good";
                    }
                    return row;
                }).ToList()
            };

            return View(vm);
        }

        private static List<int> ParseIds(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return new List<int>();
            return raw.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => int.TryParse(t.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToList();
        }
    }
}
