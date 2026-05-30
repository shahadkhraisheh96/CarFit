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

        [HttpGet]
        public async Task<IActionResult> Edit(int carId)
        {
            var vm = await _reports.LoadAsync(carId);
            if (vm == null) return NotFound();

            ViewBag.ChassisTerms = _scoring.ChassisTerms;
            ViewBag.ListingId = await GetListingIdAsync(carId);
            ViewBag.ReturnAction = "Index";
            ViewBag.ReturnController = "Listings";
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int carId, InspectionReportFormViewModel vm)
        {
            vm.CarId = carId;
            if (!ModelState.IsValid)
            {
                ViewBag.ChassisTerms = _scoring.ChassisTerms;
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
