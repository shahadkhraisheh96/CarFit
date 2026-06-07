using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>Result of a listing mutation. Carries the saved listing when relevant so the caller can chain follow-ups (e.g. image upload).</summary>
    public record ListingMutationResult(bool Ok, string Message, CarListing? Listing = null);

    /// <summary>
    /// Listing CRUD + ownership enforcement (FR-3.1/3.3/3.5, FR-7.2). All
    /// mutating methods that accept a <c>requiredSellerId</c> enforce dealer
    /// ownership when non-null; passing null is the admin path.
    /// </summary>
    public interface IListingService
    {
        /// <summary>Returns the Seller row keyed by the current user when it's approved; null otherwise.</summary>
        Task<Models.Seller?> GetApprovedSellerAsync(string userId);

        /// <summary>
        /// Get-or-create the auto-approved "Private" Seller row that lets a non-dealer (Buyer)
        /// own listings. Phase-9 buyer pay-per-post path. Idempotent per user.
        /// </summary>
        Task<Models.Seller> GetOrCreatePrivateSellerAsync(string userId, string? name, string? email, string? phone);

        /// <summary>Creates a Car + a CarListing in one go. Sellers submit as PendingInspectionReview; admin can create as Approved.</summary>
        Task<ListingMutationResult> CreateAsync(CarListingFormViewModel vm, int sellerId, string status);

        /// <summary>Updates the Car + CarListing. <paramref name="requiredSellerId"/> null = admin (no ownership check).</summary>
        Task<ListingMutationResult> UpdateAsync(int listingId, CarListingFormViewModel vm, int? requiredSellerId);

        /// <summary>Marks the listing Sold (FR-3.5). Ownership-checked when sellerId is non-null.</summary>
        Task<ListingMutationResult> DeactivateAsync(int listingId, int? requiredSellerId);

        /// <summary>Hard-deletes the listing, the orphan Car (if no other listings hold it), and any SavedResults rows.</summary>
        Task<ListingMutationResult> DeleteAsync(int listingId, int? requiredSellerId);

        /// <summary>
        /// Admin approval (FR-7.2 + Phase 10): flips Status to Approved (publicly visible).
        /// A Used car can only be approved once a structured <see cref="InspectionReport"/>
        /// exists for it; New cars need none. Enforced server-side here.
        /// </summary>
        Task<ListingMutationResult> ApproveAsync(int listingId);

        /// <summary>Admin rejection (Phase 10): sets Status to Rejected and stores the free-text reason on the listing.</summary>
        Task<ListingMutationResult> RejectAsync(int listingId, string reason);

        /// <summary>Paged listings for a single seller (dealer Inventory page).</summary>
        Task<PaginatedList<CarListing>> ListForSellerAsync(int sellerId, int page, int pageSize);

        /// <summary>Paged listings across all sellers with optional status + seller-type + condition filters (admin Listings page). Newest first.</summary>
        Task<PaginatedList<CarListing>> ListAllAsync(int page, int pageSize, string? statusFilter = null, string? sellerType = null, string? condition = null);

        /// <summary>Loads a listing with Car + CarImages for the edit form; null check enforces ownership when requested.</summary>
        Task<CarListing?> GetForFormAsync(int listingId, int? requiredSellerId);
    }

    public class ListingService : IListingService
    {
        private readonly CarFitDbContext _context;

        public ListingService(CarFitDbContext context)
        {
            _context = context;
        }

        public Task<Models.Seller?> GetApprovedSellerAsync(string userId)
            => _context.Sellers.FirstOrDefaultAsync(s => s.IdentityUserId == userId && s.IsApproved);

        public async Task<Models.Seller> GetOrCreatePrivateSellerAsync(string userId, string? name, string? email, string? phone)
        {
            var existing = await _context.Sellers.FirstOrDefaultAsync(s => s.IdentityUserId == userId);
            if (existing is not null) return existing;

            var seller = new Models.Seller
            {
                Name = string.IsNullOrWhiteSpace(name) ? (email ?? "Private Seller") : name,
                Email = email,
                Phone = phone,
                Type = "Private",
                IdentityUserId = userId,
                IsApproved = true // private sellers don't need dealer admin approval
            };
            _context.Sellers.Add(seller);
            await _context.SaveChangesAsync();
            return seller;
        }

        public async Task<ListingMutationResult> CreateAsync(CarListingFormViewModel vm, int sellerId, string status)
        {
            var car = new Car();
            ApplyCarFields(car, vm);
            _context.Cars.Add(car);
            await _context.SaveChangesAsync();

            var listing = new CarListing
            {
                CarId = car.Id,
                SellerId = sellerId,
                ListingPrice = vm.ListingPrice,
                PaymentMethodAllowed = vm.PaymentMethodAllowed,
                InstallmentOption = vm.InstallmentOption,
                Status = status,
                CreatedAt = DateTime.UtcNow
            };
            _context.CarListings.Add(listing);
            await _context.SaveChangesAsync();

            return new ListingMutationResult(true, "Listing created.", listing);
        }

        public async Task<ListingMutationResult> UpdateAsync(int listingId, CarListingFormViewModel vm, int? requiredSellerId)
        {
            var listing = await _context.CarListings
                .Include(l => l.Car)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return new ListingMutationResult(false, "Listing not found.");
            if (requiredSellerId.HasValue && listing.SellerId != requiredSellerId.Value)
                return new ListingMutationResult(false, "You can only edit your own listings.");
            if (listing.Car == null) return new ListingMutationResult(false, "Listing has no associated car.");

            ApplyCarFields(listing.Car, vm);

            listing.ListingPrice = vm.ListingPrice;
            listing.PaymentMethodAllowed = vm.PaymentMethodAllowed;
            listing.InstallmentOption = vm.InstallmentOption;
            if (!requiredSellerId.HasValue && !string.IsNullOrEmpty(vm.Status))
            {
                listing.Status = vm.Status;
            }

            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing updated.", listing);
        }

        public async Task<ListingMutationResult> DeactivateAsync(int listingId, int? requiredSellerId)
        {
            var listing = await _context.CarListings.FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null) return new ListingMutationResult(false, "Listing not found.");
            if (requiredSellerId.HasValue && listing.SellerId != requiredSellerId.Value)
                return new ListingMutationResult(false, "You can only deactivate your own listings.");

            listing.Status = ListingStatus.Sold;
            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing marked as sold.", listing);
        }

        public async Task<ListingMutationResult> DeleteAsync(int listingId, int? requiredSellerId)
        {
            var listing = await _context.CarListings
                .Include(l => l.Car)
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return new ListingMutationResult(false, "Listing not found.");
            if (requiredSellerId.HasValue && listing.SellerId != requiredSellerId.Value)
                return new ListingMutationResult(false, "You can only delete your own listings.");

            // Remove dependent rows that don't cascade automatically.
            var savedResults = await _context.SavedResults
                .Where(s => s.CarId == listing.CarId)
                .ToListAsync();
            if (savedResults.Count > 0) _context.SavedResults.RemoveRange(savedResults);

            // CarImages cascade by FK; Cars are removed too if no other listings hold them.
            _context.CarListings.Remove(listing);
            if (listing.Car != null)
            {
                var otherListings = await _context.CarListings
                    .AnyAsync(l => l.CarId == listing.CarId && l.Id != listing.Id);
                if (!otherListings)
                {
                    var report = await _context.InspectionReports.FirstOrDefaultAsync(r => r.CarId == listing.CarId);
                    if (report != null) _context.InspectionReports.Remove(report);

                    _context.Cars.Remove(listing.Car);
                }
            }

            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing removed.");
        }

        public async Task<ListingMutationResult> ApproveAsync(int listingId)
        {
            var listing = await _context.CarListings
                .Include(l => l.Car)
                .FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null) return new ListingMutationResult(false, "Listing not found.");
            if (listing.Status == ListingStatus.Approved) return new ListingMutationResult(true, "Already approved.", listing);

            // Used cars require a completed structured inspection report before they can go public.
            var isUsed = string.Equals(listing.Car?.Type, "Used", StringComparison.OrdinalIgnoreCase);
            if (isUsed)
            {
                var hasReport = listing.CarId.HasValue
                    && await _context.InspectionReports.AnyAsync(r => r.CarId == listing.CarId.Value);
                if (!hasReport)
                {
                    return new ListingMutationResult(false,
                        "Fill in and save the inspection report before approving a used car.", listing);
                }
            }

            listing.Status = ListingStatus.Approved;
            listing.RejectionReason = null; // clear any prior rejection note on re-approval
            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing approved.", listing);
        }

        public async Task<ListingMutationResult> RejectAsync(int listingId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return new ListingMutationResult(false, "A rejection reason is required.");

            var listing = await _context.CarListings.FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null) return new ListingMutationResult(false, "Listing not found.");

            listing.Status = ListingStatus.Rejected;
            listing.RejectionReason = reason.Trim().Length > 1000 ? reason.Trim()[..1000] : reason.Trim();
            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing rejected.", listing);
        }

        public Task<PaginatedList<CarListing>> ListForSellerAsync(int sellerId, int page, int pageSize)
        {
            var q = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                .Where(l => l.SellerId == sellerId)
                .OrderByDescending(l => l.Id);
            return PaginatedList<CarListing>.CreateAsync(q, page, pageSize);
        }

        public Task<PaginatedList<CarListing>> ListAllAsync(int page, int pageSize, string? statusFilter = null, string? sellerType = null, string? condition = null)
        {
            var q = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Seller)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                q = q.Where(l => l.Status == statusFilter);
            }

            // Condition filter (New/Used) maps to Car.Type.
            if (!string.IsNullOrWhiteSpace(condition))
            {
                q = q.Where(l => l.Car != null && l.Car.Type == condition);
            }

            // Seller type: private sellers carry Type == "Private"; everyone else is a Dealer.
            if (string.Equals(sellerType, "User", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(l => l.Seller != null && l.Seller.Type == "Private");
            }
            else if (string.Equals(sellerType, "Dealer", StringComparison.OrdinalIgnoreCase))
            {
                q = q.Where(l => l.Seller != null && (l.Seller.Type == null || l.Seller.Type != "Private"));
            }

            return PaginatedList<CarListing>.CreateAsync(
                q.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id), page, pageSize);
        }

        public Task<CarListing?> GetForFormAsync(int listingId, int? requiredSellerId)
        {
            var q = _context.CarListings
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .AsQueryable();
            if (requiredSellerId.HasValue)
            {
                q = q.Where(l => l.SellerId == requiredSellerId.Value);
            }
            return q.FirstOrDefaultAsync(l => l.Id == listingId);
        }

        private static void ApplyCarFields(Car car, CarListingFormViewModel vm)
        {
            car.Make = vm.Make.Trim();
            car.Model = vm.Model.Trim();
            car.Year = vm.Year;
            car.Type = vm.Type;
            car.Trim = string.IsNullOrWhiteSpace(vm.Trim) ? null : vm.Trim.Trim();
            car.Price = vm.Price;
            car.EngineSize = vm.EngineSize;
            car.FuelType = vm.FuelType;
            car.Transmission = vm.Transmission;
            car.BodyType = vm.BodyType;
            car.Seats = vm.Seats;
            car.Kilometers = vm.Kilometers;
            car.ExteriorColor = vm.ExteriorColor;
            car.InteriorColor = vm.InteriorColor;
            car.InteriorOptions = vm.InteriorOptions;
            car.ExteriorOptions = vm.ExteriorOptions;
            car.TechnologyOptions = vm.TechnologyOptions;
        }
    }
}
