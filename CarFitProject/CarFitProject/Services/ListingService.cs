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

        /// <summary>Creates a Car + a CarListing in one go. Dealers create "Pending"; admin can create "Active".</summary>
        Task<ListingMutationResult> CreateAsync(CarListingFormViewModel vm, int sellerId, string status);

        /// <summary>Updates the Car + CarListing. <paramref name="requiredSellerId"/> null = admin (no ownership check).</summary>
        Task<ListingMutationResult> UpdateAsync(int listingId, CarListingFormViewModel vm, int? requiredSellerId);

        /// <summary>Marks the listing Sold (FR-3.5). Ownership-checked when sellerId is non-null.</summary>
        Task<ListingMutationResult> DeactivateAsync(int listingId, int? requiredSellerId);

        /// <summary>Hard-deletes the listing, the orphan Car (if no other listings hold it), and any SavedResults rows.</summary>
        Task<ListingMutationResult> DeleteAsync(int listingId, int? requiredSellerId);

        /// <summary>Admin approval (FR-7.2): flips Status Pending -&gt; Active.</summary>
        Task<ListingMutationResult> ApproveAsync(int listingId);

        /// <summary>Paged listings for a single seller (dealer Inventory page).</summary>
        Task<PaginatedList<CarListing>> ListForSellerAsync(int sellerId, int page, int pageSize);

        /// <summary>Paged listings across all sellers with an optional status filter (admin Listings page).</summary>
        Task<PaginatedList<CarListing>> ListAllAsync(int page, int pageSize, string? statusFilter = null);

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
                Status = status
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

            listing.Status = "Sold";
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
                if (!otherListings) _context.Cars.Remove(listing.Car);
            }

            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing removed.");
        }

        public async Task<ListingMutationResult> ApproveAsync(int listingId)
        {
            var listing = await _context.CarListings.FirstOrDefaultAsync(l => l.Id == listingId);
            if (listing == null) return new ListingMutationResult(false, "Listing not found.");
            if (listing.Status == "Active") return new ListingMutationResult(true, "Already active.", listing);

            listing.Status = "Active";
            await _context.SaveChangesAsync();
            return new ListingMutationResult(true, "Listing approved.", listing);
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

        public Task<PaginatedList<CarListing>> ListAllAsync(int page, int pageSize, string? statusFilter = null)
        {
            var q = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                .Include(l => l.Seller)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                q = q.Where(l => l.Status == statusFilter);
            }
            return PaginatedList<CarListing>.CreateAsync(q.OrderByDescending(l => l.Id), page, pageSize);
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
