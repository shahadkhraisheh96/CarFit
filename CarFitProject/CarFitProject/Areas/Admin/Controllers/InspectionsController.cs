using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InspectionsController : Controller
    {
        private const int PageSize = 20;

        private readonly CarFitDbContext _context;
        private readonly IInspectionReportService _reports;
        private readonly IInspectionScoringService _scoring;

        public InspectionsController(
            CarFitDbContext context,
            IInspectionReportService reports,
            IInspectionScoringService scoring)
        {
            _context = context;
            _reports = reports;
            _scoring = scoring;
        }

        // GET /Admin/Inspections — all listings with their inspection-report status.
        // has = "yes" | "no" filters by whether a report is attached.
        [HttpGet]
        public async Task<IActionResult> Index(string? has = null, int page = 1)
        {
            var q = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car).ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .AsQueryable();

            if (has == "yes") q = q.Where(l => l.Car != null && l.Car.InspectionReport != null);
            else if (has == "no") q = q.Where(l => l.Car == null || l.Car.InspectionReport == null);

            q = q.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id);

            ViewBag.HasFilter = has;
            var list = await PaginatedList<CarListing>.CreateAsync(q, page, PageSize);
            return View(list);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int carId)
        {
            var vm = await _reports.LoadAsync(carId);
            if (vm == null) return NotFound();

            ViewBag.ChassisTerms = await _context.InspectionTermsGlossaries.Select(g => g.Term).ToListAsync();
            ViewBag.ListingId = await GetListingIdAsync(carId);
            ViewBag.ReturnAction = "Index";
            ViewBag.ReturnController = "Listings";
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int carId, InspectionReportFormViewModel vm, int? returnListingId = null)
        {
            vm.CarId = carId;
            if (!ModelState.IsValid)
            {
                ViewBag.ChassisTerms = await _context.InspectionTermsGlossaries.Select(g => g.Term).ToListAsync();
                ViewBag.ListingId = await GetListingIdAsync(carId);
                ViewBag.ReturnAction = "Index";
                ViewBag.ReturnController = "Listings";
                return View("Form", vm);
            }

            var saved = await _reports.SaveAsync(carId, vm);
            if (!saved)
            {
                TempData["ErrorMessage"] = "Couldn't find that car.";
                return RedirectToAction("Index", "Listings");
            }

            TempData["SuccessMessage"] = $"Inspection saved (score {vm.OverallScore:0.00}).";
            // When the admin saved from the review page, return there so they can approve.
            if (returnListingId.HasValue)
            {
                return RedirectToAction("Review", "Listings", new { id = returnListingId.Value });
            }
            return RedirectToAction("Index", "Listings");
        }

        private Task<int?> GetListingIdAsync(int carId)
            => _context.CarListings
                .AsNoTracking()
                .Where(l => l.CarId == carId)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
    }
}
