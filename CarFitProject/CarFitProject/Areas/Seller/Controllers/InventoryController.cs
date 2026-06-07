using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.Services.Billing;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarFitProject.Areas.Seller.Controllers
{
    // Dealers manage their lot here; Buyers (private sellers) reuse the same form to post a car
    // under the Phase-9 trial / pay-per-post flow.
    [Area("Seller")]
    [Authorize(Roles = "Dealer,Buyer")]
    public class InventoryController : Controller
    {
        private const int MinImages = 3;
        private const int MaxImages = 15;
        private const int PageSize = 12;

        private readonly CarFitDbContext _context;
        private readonly IListingService _listings;
        private readonly IImageStorageService _images;
        private readonly IInspectionUploadService _inspectionUploads;
        private readonly ISellerSubscriptionService _subs;
        private readonly UserManager<ApplicationUser> _userManager;

        public InventoryController(
            CarFitDbContext context,
            IListingService listings,
            IImageStorageService images,
            IInspectionUploadService inspectionUploads,
            ISellerSubscriptionService subs,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _listings = listings;
            _images = images;
            _inspectionUploads = inspectionUploads;
            _subs = subs;
            _userManager = userManager;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private bool IsPrivateSeller => User.IsInRole("Buyer") && !User.IsInRole("Dealer");

        /// <summary>
        /// Phase-9 listing gate. Dealers need an active subscription. Buyers (private sellers) may
        /// post when their free trial is in-window (an active Trial subscription) OR they hold a
        /// prepaid per-post credit. Enforced on both the GET form and the POST create path so a
        /// crafted direct POST can't bypass it.
        /// </summary>
        private async Task<bool> HasListingAccessAsync()
        {
            if (string.IsNullOrEmpty(UserId)) return false;
            if (await _subs.HasActiveSubscriptionAsync(UserId)) return true; // active sub or in-window trial
            if (await _subs.CountUnconsumedPerPostCreditsAsync(UserId) > 0) return true;
            return false;
        }

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

        // Backward-compatibility stubs: the add/edit form moved to the unified
        // Form action. Permanent-redirect any old AddCar/Edit links there.
        [HttpGet]
        public IActionResult AddCar() => RedirectToActionPermanent(nameof(Form));

        [HttpGet]
        public IActionResult Edit(int id) => RedirectToActionPermanent(nameof(Form), new { id });

        // GET: /Seller/Inventory/Form          → new listing
        // GET: /Seller/Inventory/Form/{id}     → edit existing listing
        [HttpGet]
        public async Task<IActionResult> Form(int? id)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            if (id == null)
            {
                if (!await HasListingAccessAsync())
                {
                    TempData["ErrorMessage"] = "You need an active subscription or a per-listing credit to publish a listing.";
                    return RedirectToAction("Index", "Billing", new { area = "" });
                }
                return View(new CarListingFormViewModel { Year = DateTime.UtcNow.Year, Type = "Used" });
            }

            var listing = await _listings.GetForFormAsync(id.Value, seller.Id);
            if (listing == null || listing.Car == null) return NotFound();

            var formVm = MapToForm(listing);
            formVm.ExistingInspectionUploads = await _inspectionUploads.GetForCarAsync(listing.CarId!.Value);
            return View(formVm);
        }

        // POST: /Seller/Inventory/Form          → create (submitted as Pending)
        // POST: /Seller/Inventory/Form/{id}     → update
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestFormLimits(MultipartBodyLengthLimit = 320_000_000)] // images + up to 10×25MB inspection files
        public async Task<IActionResult> Form(int? id, CarListingFormViewModel vm, List<IFormFile> images, List<IFormFile> inspectionFiles)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var isUsed = string.Equals(vm.Type, "Used", StringComparison.OrdinalIgnoreCase);
            var newInspectionCount = inspectionFiles?.Count(f => f.Length > 0) ?? 0;

            if (id == null)
            {
                // Listing gate (re-checked on POST so a crafted request can't skip the subscription).
                if (!await HasListingAccessAsync())
                {
                    TempData["ErrorMessage"] = "You need an active subscription or a per-listing credit to publish a listing.";
                    return RedirectToAction("Index", "Billing", new { area = "" });
                }

                ValidateImageCount(images?.Count ?? 0, 0);
                // Used cars must include at least one inspection document (unless requesting one).
                if (isUsed && newInspectionCount == 0 && !vm.RequestInspection)
                {
                    ModelState.AddModelError("inspectionFiles",
                        "Used cars require at least one inspection document (PDF, JPG, PNG, or WebP).");
                }
                if (!ModelState.IsValid) return View(vm);

                var result = await _listings.CreateAsync(vm, seller.Id, status: ListingStatus.PendingInspectionReview);
                if (!result.Ok || result.Listing == null)
                {
                    ModelState.AddModelError(string.Empty, result.Message);
                    return View(vm);
                }

                // Persist inspection evidence first; if it fails validation, roll the listing back
                // so we never leave a Used car with zero uploads.
                if (newInspectionCount > 0)
                {
                    var upload = await _inspectionUploads.SaveUploadsAsync(result.Listing.CarId!.Value, inspectionFiles, UserId);
                    if (!upload.Ok)
                    {
                        await _listings.DeleteAsync(result.Listing.Id, seller.Id);
                        ModelState.AddModelError("inspectionFiles", upload.Message);
                        return View(vm);
                    }
                }

                await _images.SaveImagesAsync(result.Listing.CarId!.Value,
                    images ?? new List<IFormFile>(), startSortOrder: 0, makeFirstPrimary: true);

                // Users not on an active trial/sub paid per-post: spend one credit on this listing.
                if (!await _subs.HasActiveSubscriptionAsync(UserId!))
                {
                    await _subs.ConsumePerPostCreditAsync(UserId!, result.Listing.Id);
                }

                if (vm.RequestInspection)
                {
                    var user = await _userManager.FindByIdAsync(UserId!);
                    if (user != null)
                    {
                        _context.InspectionBookings.Add(new InspectionBooking
                        {
                            CustomerName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email!,
                            CustomerEmail = user.Email!,
                            PackageType = "Seller Listing Inspection",
                            PreferredDate = DateTime.UtcNow.AddDays(1).Date,
                            CarListingId = result.Listing.Id,
                            VehicleNotes = "Seller requested inspection for new listing.",
                            Status = "Pending"
                        });
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["SuccessMessage"] = "Listing submitted for review.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _listings.GetForFormAsync(id.Value, seller.Id);
            if (existing == null || existing.Car == null) return NotFound();

            var existingImageCount = existing.Car.CarImages?.Count ?? 0;
            ValidateImageCount(images?.Count ?? 0, existingImageCount);

            var existingInspectionCount = await _context.SellerInspectionUploads
                .CountAsync(u => u.CarId == existing.CarId);
            // If the listing is (or is being switched to) Used, it must end up with ≥1 inspection file or request.
            if (isUsed && existingInspectionCount == 0 && newInspectionCount == 0 && !vm.RequestInspection)
            {
                ModelState.AddModelError("inspectionFiles",
                    "Used cars require at least one inspection document (PDF, JPG, PNG, or WebP).");
            }

            if (!ModelState.IsValid)
            {
                vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                vm.ExistingInspectionUploads = await _inspectionUploads.GetForCarAsync(existing.CarId!.Value);
                return View(vm);
            }

            vm.ListingId = id;
            vm.CarId = existing.CarId;
            var update = await _listings.UpdateAsync(id.Value, vm, seller.Id);
            if (!update.Ok)
            {
                ModelState.AddModelError(string.Empty, update.Message);
                vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                vm.ExistingInspectionUploads = await _inspectionUploads.GetForCarAsync(existing.CarId!.Value);
                return View(vm);
            }

            if (newInspectionCount > 0)
            {
                var upload = await _inspectionUploads.SaveUploadsAsync(existing.CarId!.Value, inspectionFiles, UserId);
                if (!upload.Ok)
                {
                    ModelState.AddModelError("inspectionFiles", upload.Message);
                    vm.ExistingImages = existing.Car.CarImages?.OrderBy(i => i.SortOrder).ToList() ?? new();
                    vm.ExistingInspectionUploads = await _inspectionUploads.GetForCarAsync(existing.CarId!.Value);
                    return View(vm);
                }
            }

            if (images != null && images.Count > 0)
            {
                await _images.SaveImagesAsync(existing.CarId!.Value,
                    images, startSortOrder: existingImageCount, makeFirstPrimary: existingImageCount == 0);
            }

            if (vm.RequestInspection)
            {
                var existingBooking = await _context.InspectionBookings
                    .AnyAsync(b => b.CarListingId == id.Value && (b.Status == "Pending" || b.Status == "Confirmed"));
                
                if (!existingBooking)
                {
                    var user = await _userManager.FindByIdAsync(UserId!);
                    if (user != null)
                    {
                        _context.InspectionBookings.Add(new InspectionBooking
                        {
                            CustomerName = !string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email!,
                            CustomerEmail = user.Email!,
                            PackageType = "Seller Listing Inspection",
                            PreferredDate = DateTime.UtcNow.AddDays(1).Date,
                            CarListingId = id.Value,
                            VehicleNotes = "Seller requested inspection on listing edit.",
                            Status = "Pending"
                        });
                        await _context.SaveChangesAsync();
                    }
                }
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
            return RedirectToAction(nameof(Form), new { id = listingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MakePrimaryImage(int id, int listingId)
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

            await _images.SetPrimaryAsync(id);
            TempData["SuccessMessage"] = "Primary image updated.";
            return RedirectToAction(nameof(Form), new { id = listingId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteInspectionUpload(int id, int listingId)
        {
            var seller = await RequireApprovedSellerAsync();
            if (seller is null) return RedirectToAction("Index", "Dashboard");

            var upload = await _context.SellerInspectionUploads
                .Include(u => u.Car)
                    .ThenInclude(c => c.CarListings)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (upload == null) return NotFound();
            if (!upload.Car.CarListings.Any(l => l.SellerId == seller.Id)) return Forbid();

            // A Used car must keep at least one inspection document.
            var isUsed = string.Equals(upload.Car.Type, "Used", StringComparison.OrdinalIgnoreCase);
            var remaining = await _context.SellerInspectionUploads.CountAsync(u => u.CarId == upload.CarId);
            if (isUsed && remaining <= 1)
            {
                TempData["ErrorMessage"] = "A used car must keep at least one inspection document. Upload a replacement first.";
                return RedirectToAction(nameof(Form), new { id = listingId });
            }

            await _inspectionUploads.DeleteAsync(id);
            TempData["SuccessMessage"] = "Inspection document removed.";
            return RedirectToAction(nameof(Form), new { id = listingId });
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------
        private async Task<Models.Seller?> RequireApprovedSellerAsync()
        {
            if (string.IsNullOrEmpty(UserId)) return null;

            // Buyers post as auto-approved private sellers — no dealer approval gate.
            if (IsPrivateSeller)
            {
                var user = await _userManager.GetUserAsync(User);
                return await _listings.GetOrCreatePrivateSellerAsync(
                    UserId, user?.FullName, user?.Email, user?.PhoneNumber);
            }

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
