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
    public class ListingsController : Controller
    {
        private const int MinImages = 3;
        private const int MaxImages = 15;
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;
        private readonly IListingService _listings;
        private readonly IImageStorageService _images;

        public ListingsController(
            CarFitDbContext context,
            IListingService listings,
            IImageStorageService images)
        {
            _context = context;
            _listings = listings;
            _images = images;
        }

        public async Task<IActionResult> Index(string? status = null, int page = 1)
        {
            ViewBag.StatusFilter = status;
            var listings = await _listings.ListAllAsync(page, PageSize, status);
            return View(listings);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.AvailableSellers = await SellerListAsync();
            return View("Form", new CarListingFormViewModel { Year = DateTime.UtcNow.Year, Type = "Used", Status = "Active" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> Create(CarListingFormViewModel vm, List<IFormFile> images, int sellerId)
        {
            ValidateImageCount(images?.Count ?? 0, 0);
            if (sellerId <= 0)
            {
                ModelState.AddModelError("sellerId", "Please pick a dealer.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.AvailableSellers = await SellerListAsync();
                return View("Form", vm);
            }

            var status = string.IsNullOrWhiteSpace(vm.Status) ? "Active" : vm.Status;
            var result = await _listings.CreateAsync(vm, sellerId, status);
            if (!result.Ok || result.Listing == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.AvailableSellers = await SellerListAsync();
                return View("Form", vm);
            }

            await _images.SaveImagesAsync(result.Listing.CarId!.Value,
                images ?? new List<IFormFile>(), startSortOrder: 0, makeFirstPrimary: true);

            TempData["SuccessMessage"] = "Listing created.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var listing = await _listings.GetForFormAsync(id, requiredSellerId: null);
            if (listing == null || listing.Car == null) return NotFound();

            ViewBag.AvailableSellers = await SellerListAsync();
            ViewBag.CurrentSellerId = listing.SellerId;
            var vm = new CarListingFormViewModel
            {
                ListingId = listing.Id,
                CarId = listing.CarId,
                Make = listing.Car.Make,
                Model = listing.Car.Model,
                Year = listing.Car.Year,
                Type = listing.Car.Type ?? "Used",
                Trim = listing.Car.Trim,
                Price = listing.Car.Price,
                EngineSize = listing.Car.EngineSize,
                FuelType = listing.Car.FuelType,
                Transmission = listing.Car.Transmission ?? "Automatic",
                BodyType = listing.Car.BodyType,
                Seats = listing.Car.Seats,
                Kilometers = listing.Car.Kilometers,
                ExteriorColor = listing.Car.ExteriorColor,
                InteriorColor = listing.Car.InteriorColor,
                InteriorOptions = listing.Car.InteriorOptions,
                ExteriorOptions = listing.Car.ExteriorOptions,
                TechnologyOptions = listing.Car.TechnologyOptions,
                ListingPrice = listing.ListingPrice ?? 0m,
                PaymentMethodAllowed = listing.PaymentMethodAllowed,
                InstallmentOption = listing.InstallmentOption ?? false,
                Status = listing.Status,
                ExistingImages = listing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new()
            };
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> Edit(int id, CarListingFormViewModel vm, List<IFormFile> images)
        {
            var existing = await _listings.GetForFormAsync(id, requiredSellerId: null);
            if (existing == null || existing.Car == null) return NotFound();

            var existingCount = existing.Car.CarImages?.Count ?? 0;
            ValidateImageCount(images?.Count ?? 0, existingCount);

            if (!ModelState.IsValid)
            {
                ViewBag.AvailableSellers = await SellerListAsync();
                ViewBag.CurrentSellerId = existing.SellerId;
                vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                return View("Form", vm);
            }

            vm.ListingId = id;
            vm.CarId = existing.CarId;
            var result = await _listings.UpdateAsync(id, vm, requiredSellerId: null);
            if (!result.Ok)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                ViewBag.AvailableSellers = await SellerListAsync();
                ViewBag.CurrentSellerId = existing.SellerId;
                vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                return View("Form", vm);
            }

            if (images != null && images.Count > 0)
            {
                await _images.SaveImagesAsync(existing.CarId!.Value,
                    images, startSortOrder: existingCount, makeFirstPrimary: existingCount == 0);
            }

            TempData["SuccessMessage"] = "Listing updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _listings.ApproveAsync(id);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _listings.DeleteAsync(id, requiredSellerId: null);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int listingId)
        {
            await _images.DeleteAsync(id);
            TempData["SuccessMessage"] = "Image removed.";
            return RedirectToAction(nameof(Edit), new { id = listingId });
        }

        private void ValidateImageCount(int newCount, int existingCount)
        {
            var total = newCount + existingCount;
            if (existingCount == 0 && newCount < MinImages)
            {
                ModelState.AddModelError("images", $"Please upload at least {MinImages} photos.");
            }
            if (total > MaxImages)
            {
                ModelState.AddModelError("images", $"You can have at most {MaxImages} photos per listing (currently {total}).");
            }
        }

        private Task<List<CarFitProject.Models.Seller>> SellerListAsync()
            => _context.Sellers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
    }
}
