using CarFitProject.Helpers;
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
    public class InventoryController : Controller
    {
        private const int MinImages = 3;
        private const int MaxImages = 15;
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;
        private readonly IListingService _listings;
        private readonly IImageStorageService _images;

        public InventoryController(
            CarFitDbContext context,
            IListingService listings,
            IImageStorageService images)
        {
            _context = context;
            _listings = listings;
            _images = images;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        public async Task<IActionResult> Index(int page = 1)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var sellerId = await _context.Sellers
                .Where(s => s.IdentityUserId == UserId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            if (sellerId == null)
            {
                return View(new PaginatedList<CarListing>(new List<CarListing>(), 0, 1, PageSize));
            }

            var listings = await _listings.ListForSellerAsync(sellerId.Value, page, PageSize);
            return View(listings);
        }

        [HttpGet]
        public async Task<IActionResult> AddCar()
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            return View("Form", new CarListingFormViewModel { Year = DateTime.UtcNow.Year, Type = "Used" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> AddCar(CarListingFormViewModel vm, List<IFormFile> images)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            ValidateImageCount(images?.Count ?? 0, 0);
            if (!ModelState.IsValid) return View("Form", vm);

            var result = await _listings.CreateAsync(vm, seller.Id, status: "Pending");
            if (!result.Ok || result.Listing == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View("Form", vm);
            }

            await _images.SaveImagesAsync(result.Listing.CarId!.Value,
                images ?? new List<IFormFile>(), startSortOrder: 0, makeFirstPrimary: true);

            TempData["SuccessMessage"] = "Listing submitted for review.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var listing = await _listings.GetForFormAsync(id, seller.Id);
            if (listing == null || listing.Car == null) return NotFound();

            var vm = MapToForm(listing);
            return View("Form", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 60_000_000)]
        public async Task<IActionResult> Edit(int id, CarListingFormViewModel vm, List<IFormFile> images)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var existing = await _listings.GetForFormAsync(id, seller.Id);
            if (existing == null || existing.Car == null) return NotFound();

            var existingCount = existing.Car.CarImages?.Count ?? 0;
            ValidateImageCount(images?.Count ?? 0, existingCount);

            if (!ModelState.IsValid)
            {
                vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                return View("Form", vm);
            }

            vm.ListingId = id;
            vm.CarId = existing.CarId;
            var result = await _listings.UpdateAsync(id, vm, seller.Id);
            if (!result.Ok)
            {
                ModelState.AddModelError(string.Empty, result.Message);
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
        public async Task<IActionResult> Deactivate(int id)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var result = await _listings.DeactivateAsync(id, seller.Id);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var result = await _listings.DeleteAsync(id, seller.Id);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int id, int listingId)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var image = await _context.CarImages
                .Include(i => i.Car)
                    .ThenInclude(c => c!.CarListings)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (image == null || image.Car == null) return NotFound();
            if (!image.Car.CarListings.Any(l => l.SellerId == seller.Id))
            {
                return Forbid();
            }

            await _images.DeleteAsync(id);
            TempData["SuccessMessage"] = "Image removed.";
            return RedirectToAction(nameof(Edit), new { id = listingId });
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private async Task<Models.Seller?> RequireApprovedSellerAsync()
        {
            if (string.IsNullOrEmpty(UserId)) return null;
            var seller = await _listings.GetApprovedSellerAsync(UserId);
            if (seller == null)
            {
                TempData["ErrorMessage"] = "Your dealership is awaiting admin approval — you can't list vehicles yet.";
            }
            return seller;
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

        private static CarListingFormViewModel MapToForm(CarListing listing)
        {
            var car = listing.Car!;
            return new CarListingFormViewModel
            {
                ListingId = listing.Id,
                CarId = listing.CarId,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                Type = car.Type ?? "Used",
                Trim = car.Trim,
                Price = car.Price,
                EngineSize = car.EngineSize,
                FuelType = car.FuelType,
                Transmission = car.Transmission ?? "Automatic",
                BodyType = car.BodyType,
                Seats = car.Seats,
                Kilometers = car.Kilometers,
                ExteriorColor = car.ExteriorColor,
                InteriorColor = car.InteriorColor,
                InteriorOptions = car.InteriorOptions,
                ExteriorOptions = car.ExteriorOptions,
                TechnologyOptions = car.TechnologyOptions,
                ListingPrice = listing.ListingPrice ?? 0m,
                PaymentMethodAllowed = listing.PaymentMethodAllowed,
                InstallmentOption = listing.InstallmentOption ?? false,
                Status = listing.Status,
                ExistingImages = car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new()
            };
        }
    }

}
