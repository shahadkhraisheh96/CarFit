using CarFitProject.Helpers;
using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public sealed record SaveToggleResult(bool Ok, bool IsSaved, string Message);

    /// <summary>
    /// Save / unsave bookmarks plus the queries that back the buyer Saved Cars
    /// page (FR-6.1). Free-tier capacity gating is delegated to
    /// <see cref="ISubscriptionService"/> on every toggle.
    /// </summary>
    public interface ISavedCarsService
    {
        /// <summary>Toggle: removes the row if it already exists; otherwise inserts one after checking the Free-plan limit.</summary>
        Task<SaveToggleResult> ToggleAsync(string userId, int carId);

        /// <summary>Saved CarIds for badge state on listing cards.</summary>
        Task<HashSet<int>> GetSavedCarIdsAsync(string userId);

        /// <summary>All saved active listings — used when paging isn't needed (e.g. backwards-compatible callers).</summary>
        Task<List<CarListing>> GetSavedListingsAsync(string userId);

        /// <summary>Paged saved-listings query (12/page default — NFR-Sc1).</summary>
        Task<PaginatedList<CarListing>> GetSavedListingsPagedAsync(string userId, int page, int pageSize = 12);

        /// <summary>Total saved-cars count for the badge in the buyer nav.</summary>
        Task<int> CountAsync(string userId);
    }

    public class SavedCarsService : ISavedCarsService
    {
        private readonly CarFitDbContext _context;
        private readonly ISubscriptionService _subscriptions;

        public SavedCarsService(CarFitDbContext context, ISubscriptionService subscriptions)
        {
            _context = context;
            _subscriptions = subscriptions;
        }

        public async Task<SaveToggleResult> ToggleAsync(string userId, int carId)
        {
            if (string.IsNullOrEmpty(userId))
                return new SaveToggleResult(false, false, "Sign in to save cars.");

            var existing = await _context.SavedResults
                .FirstOrDefaultAsync(s => s.UserId == userId && s.CarId == carId);

            if (existing != null)
            {
                _context.SavedResults.Remove(existing);
                await _context.SaveChangesAsync();
                return new SaveToggleResult(true, false, "Removed from saved cars.");
            }

            if (!await _subscriptions.CanSaveMoreAsync(userId))
            {
                return new SaveToggleResult(false, false,
                    $"You're at the {_subscriptions.FreeSaveLimit}-car limit on the Free plan — upgrade to Premium to save more.");
            }

            _context.SavedResults.Add(new SavedResult
            {
                UserId = userId,
                CarId = carId
            });
            await _context.SaveChangesAsync();
            return new SaveToggleResult(true, true, "Saved.");
        }

        public async Task<HashSet<int>> GetSavedCarIdsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new HashSet<int>();
            var ids = await _context.SavedResults
                .Where(s => s.UserId == userId)
                .Select(s => s.CarId)
                .ToListAsync();
            return new HashSet<int>(ids);
        }

        public async Task<List<CarListing>> GetSavedListingsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return new List<CarListing>();

            var ids = await _context.SavedResults
                .Where(s => s.UserId == userId)
                .Select(s => s.CarId)
                .ToListAsync();
            if (ids.Count == 0) return new List<CarListing>();

            return await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .Where(l => ids.Contains(l.CarId!.Value) && l.Status == "Active")
                .ToListAsync();
        }

        public async Task<PaginatedList<CarListing>> GetSavedListingsPagedAsync(string userId, int page, int pageSize = 12)
        {
            if (string.IsNullOrEmpty(userId))
                return new PaginatedList<CarListing>(new List<CarListing>(), 0, page, pageSize);

            var query = _context.SavedResults
                .AsNoTracking()
                .Where(s => s.UserId == userId)
                .Join(_context.CarListings.Include(l => l.Car)
                        .ThenInclude(c => c!.CarImages)
                      .Include(l => l.Car)
                        .ThenInclude(c => c!.InspectionReport)
                      .Include(l => l.Seller)
                        .Where(l => l.Status == "Active"),
                      s => s.CarId,
                      l => l.CarId!.Value,
                      (s, l) => l)
                .OrderByDescending(l => l.Id);

            return await PaginatedList<CarListing>.CreateAsync(query, page, pageSize);
        }

        public Task<int> CountAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return Task.FromResult(0);
            return _context.SavedResults.CountAsync(s => s.UserId == userId);
        }
    }
}
