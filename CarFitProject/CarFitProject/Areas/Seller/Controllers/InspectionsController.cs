using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Dealer")]
    public class InspectionsController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly IInspectionReportService _reports;
        private readonly IInspectionScoringService _scoring;
        private readonly IListingService _listings;

        public InspectionsController(
            CarFitDbContext context,
            IInspectionReportService reports,
            IInspectionScoringService scoring,
            IListingService listings)
        {
            _context = context;
            _reports = reports;
            _scoring = scoring;
            _listings = listings;
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int carId)
        {
            var seller = await RequireOwningSellerAsync(carId);
            if (seller == null) return RedirectToAction("Index", "Inventory");

            var vm = await _reports.LoadAsync(carId);
            if (vm == null) return NotFound();

            ViewBag.ChassisTerms = _scoring.ChassisTerms;
            ViewBag.ListingId = await GetListingIdAsync(carId);
            ViewBag.ReturnAction = "Index";
            ViewBag.ReturnController = "Inventory";
            return View("/Areas/Admin/Views/Inspections/Form.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int carId, InspectionReportFormViewModel vm)
        {
            var seller = await RequireOwningSellerAsync(carId);
            if (seller == null) return RedirectToAction("Index", "Inventory");

            vm.CarId = carId;
            if (!ModelState.IsValid)
            {
                ViewBag.ChassisTerms = _scoring.ChassisTerms;
                ViewBag.ListingId = await GetListingIdAsync(carId);
                ViewBag.ReturnAction = "Index";
                ViewBag.ReturnController = "Inventory";
                return View("/Areas/Admin/Views/Inspections/Form.cshtml", vm);
            }

            await _reports.SaveAsync(carId, vm);
            TempData["SuccessMessage"] = $"Inspection saved (score {vm.OverallScore:0.00}).";
            return RedirectToAction("Index", "Inventory");
        }

        private async Task<Models.Seller?> RequireOwningSellerAsync(int carId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var seller = await _listings.GetApprovedSellerAsync(userId);
            if (seller == null)
            {
                TempData["ErrorMessage"] = "Your dealership is awaiting admin approval.";
                return null;
            }

            var owns = await _context.CarListings
                .AnyAsync(l => l.CarId == carId && l.SellerId == seller.Id);
            if (!owns)
            {
                TempData["ErrorMessage"] = "You can only attach inspection reports to your own listings.";
                return null;
            }

            return seller;
        }

        private Task<int?> GetListingIdAsync(int carId)
            => _context.CarListings
                .AsNoTracking()
                .Where(l => l.CarId == carId)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
    }
}
