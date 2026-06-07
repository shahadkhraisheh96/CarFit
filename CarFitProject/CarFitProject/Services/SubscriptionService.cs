using CarFitProject.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>
    /// Subscription-tier checks that drive premium-only features
    /// (unlimited Save Car FR-6.1, email contact FR-6.3).
    /// </summary>
    public interface ISubscriptionService
    {
        /// <summary>Maximum saved cars allowed on the free tier.</summary>
        int FreeSaveLimit { get; }

        /// <summary>True when the user's <c>SubscriptionTier</c> equals "Premium".</summary>
        bool IsPremium(ApplicationUser? user);

        /// <summary>Async overload that resolves the user by Identity id first.</summary>
        Task<bool> IsPremiumAsync(string? userId);

        /// <summary>True if the user can save another car (always true for Premium, else under the free limit).</summary>
        Task<bool> CanSaveMoreAsync(string? userId);
    }

    /// <summary>
    /// Default <see cref="ISubscriptionService"/> backed by Identity's
    /// <c>SubscriptionTier</c> string and the SavedResults row count.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private const int FreeLimit = 3;

        private readonly CarFitDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubscriptionService(CarFitDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int FreeSaveLimit => FreeLimit;

        public bool IsPremium(ApplicationUser? user)
            => user != null
               && string.Equals(user.SubscriptionTier, "Premium", StringComparison.OrdinalIgnoreCase);

        public async Task<bool> IsPremiumAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            var user = await _userManager.FindByIdAsync(userId);
            return IsPremium(user);
        }

        public async Task<bool> CanSaveMoreAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return false;
            if (await IsPremiumAsync(userId)) return true;
            var count = await _context.SavedResults.CountAsync(s => s.UserId == userId);
            return count < FreeLimit;
        }
    }
}
