using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IInspectionUploadService _inspectionUploads;
        private readonly IInspectionReportService _reports;
        private readonly IInspectionScoringService _scoring;
        private readonly IEmailSender _email;
        private readonly ILogger<ListingsController> _logger;

        public ListingsController(
            CarFitDbContext context,
            IListingService listings,
            IImageStorageService images,
            IInspectionUploadService inspectionUploads,
            IInspectionReportService reports,
            IInspectionScoringService scoring,
            IEmailSender email,
            ILogger<ListingsController> logger)
        {
            _context = context;
            _listings = listings;
            _images = images;
            _inspectionUploads = inspectionUploads;
            _reports = reports;
            _scoring = scoring;
            _email = email;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? status = null, string? sellerType = null, string? condition = null, int page = 1)
        {
            ViewBag.StatusFilter = status;
            ViewBag.SellerType = sellerType;
            ViewBag.Condition = condition;
            var listings = await _listings.ListAllAsync(page, PageSize, status, sellerType, condition);
            return View(listings);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.AvailableSellers = await SellerListAsync();
            return View("Form", new CarListingFormViewModel { Year = DateTime.UtcNow.Year, Type = "Used", Status = ListingStatus.Approved });
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

            var status = string.IsNullOrWhiteSpace(vm.Status) ? ListingStatus.Approved : vm.Status;
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

        // GET /Admin/Listings/Review/{id} — the Phase-10 review page: car info, photos,
        // seller, raw inspection uploads, and the structured inspection form.
        [HttpGet]
        public async Task<IActionResult> Review(int id)
        {
            var listing = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car).ThenInclude(c => c!.CarImages)
                .Include(l => l.Car).ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (listing == null || listing.Car == null) return NotFound();

            var carId = listing.CarId!.Value;
            var vm = new ListingReviewViewModel
            {
                Listing = listing,
                InspectionUploads = await _inspectionUploads.GetForCarAsync(carId),
                InspectionForm = await _reports.LoadAsync(carId) ?? new InspectionReportFormViewModel { CarId = carId },
                HasInspectionReport = await _context.InspectionReports.AnyAsync(r => r.CarId == carId)
            };
            ViewBag.ChassisTerms = await _context.InspectionTermsGlossaries.Select(g => g.Term).ToListAsync();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _listings.ApproveAsync(id);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (result.Ok && result.Listing != null)
            {
                await NotifySellerAsync(result.Listing.Id, approved: true, reason: null);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var result = await _listings.RejectAsync(id, reason);
            TempData[result.Ok ? "SuccessMessage" : "ErrorMessage"] = result.Message;

            if (result.Ok && result.Listing != null)
            {
                await NotifySellerAsync(result.Listing.Id, approved: false, reason: reason);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Notifies the seller of an approve/reject decision via the existing IEmailSender
        /// (logs in Development, SMTP in Production) and writes an audit log line.
        /// </summary>
        private async Task NotifySellerAsync(int listingId, bool approved, string? reason)
        {
            var info = await _context.CarListings
                .AsNoTracking()
                .Where(l => l.Id == listingId)
                .Select(l => new { l.Seller!.Email, Make = l.Car!.Make, Model = l.Car!.Model, l.Car!.Year })
                .FirstOrDefaultAsync();

            var vehicle = info == null ? $"listing #{listingId}" : $"{info.Year} {info.Make} {info.Model}";
            if (approved)
            {
                _logger.LogInformation("Listing {ListingId} approved; notifying seller {Email}.", listingId, info?.Email);
            }
            else
            {
                _logger.LogInformation("Listing {ListingId} rejected (reason: {Reason}); notifying seller {Email}.", listingId, reason, info?.Email);
            }

            if (string.IsNullOrWhiteSpace(info?.Email)) return;

            try
            {
                if (approved)
                {
                    await _email.SendEmailAsync(info.Email,
                        "Your CarFit listing is approved",
                        $"Good news — your {vehicle} listing has been approved and is now visible to buyers.");
                }
                else
                {
                    await _email.SendEmailAsync(info.Email,
                        "Your CarFit listing needs changes",
                        $"Your {vehicle} listing was not approved. Reason: {reason}");
                }
            }
            catch (Exception ex)
            {
                // Never let a notification failure roll back the approve/reject decision.
                _logger.LogWarning(ex, "Failed to send listing decision email for listing {ListingId}.", listingId);
            }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePrimaryImage(int id, int listingId)
        {
            await _images.SetPrimaryAsync(id);
            TempData["SuccessMessage"] = "Primary image updated.";
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
