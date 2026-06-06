using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services.Billing
{
    /// <summary>
    /// Domain queries/commands over the Phase-9 billing tables, shared by the listing gate,
    /// the billing dashboard, the admin report and the Stripe webhook. Pure persistence logic —
    /// no Stripe API calls (those live in <see cref="IStripeService"/>).
    /// </summary>
    public interface ISellerSubscriptionService
    {
        Task<List<SubscriptionPlan>> GetActivePlansForRoleAsync(string role, CancellationToken ct = default);

        Task<SubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken ct = default);

        /// <summary>Most recent subscription row for the user (any status), Plan included.</summary>
        Task<Subscription?> GetCurrentAsync(string userId, CancellationToken ct = default);

        /// <summary>Recurring subscription currently granting access (Trial within window, or Active), Plan included.</summary>
        Task<Subscription?> GetActiveAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// True when the user holds a recurring subscription that is Active, or in a Trial whose
        /// window has not elapsed. Does NOT consider pay-per-post credits — the listing gate layers
        /// that on top (see <see cref="CountUnconsumedPerPostCreditsAsync"/>).
        /// </summary>
        Task<bool> HasActiveSubscriptionAsync(string userId, CancellationToken ct = default);

        /// <summary>Pay-per-post payments by this user that succeeded but aren't yet tied to a listing.</summary>
        Task<int> CountUnconsumedPerPostCreditsAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// Attach the oldest unconsumed pay-per-post credit to <paramref name="carListingId"/>,
        /// "spending" it. Returns false if the user had no credit to consume.
        /// </summary>
        Task<bool> ConsumePerPostCreditAsync(string userId, int carListingId, CancellationToken ct = default);

        /// <summary>
        /// Idempotently record a payment keyed by StripePaymentIntentId. Returns the row (existing or
        /// new) and whether it was newly created — callers branch on <c>created</c> for side effects
        /// (SMS, listing unlock) so re-delivered webhooks stay safe.
        /// </summary>
        Task<(PaymentTransaction tx, bool created)> RecordPaymentIdempotentAsync(
            PaymentTransaction candidate, CancellationToken ct = default);

        Task<List<PaymentTransaction>> GetPaymentHistoryAsync(string userId, CancellationToken ct = default);

        Task<decimal> GetLifetimePaidJodAsync(string userId, CancellationToken ct = default);

        /// <summary>
        /// Seller ids whose owning user currently has an active (or in-window trial) subscription —
        /// used to float subscribers to the top of public search results.
        /// </summary>
        Task<List<int>> GetActiveSubscriberSellerIdsAsync(CancellationToken ct = default);
    }

    /// <inheritdoc cref="ISellerSubscriptionService"/>
    public class SellerSubscriptionService : ISellerSubscriptionService
    {
        private readonly CarFitDbContext _context;

        public SellerSubscriptionService(CarFitDbContext context) => _context = context;

        public Task<List<SubscriptionPlan>> GetActivePlansForRoleAsync(string role, CancellationToken ct = default) =>
            _context.SubscriptionPlans
                .Where(p => p.IsActive && p.TargetRole == role)
                .OrderBy(p => p.AmountJod)
                .ToListAsync(ct);

        public Task<SubscriptionPlan?> GetPlanByCodeAsync(string planCode, CancellationToken ct = default) =>
            _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanCode == planCode, ct);

        public Task<Subscription?> GetCurrentAsync(string userId, CancellationToken ct = default) =>
            _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);

        public Task<Subscription?> GetActiveAsync(string userId, CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            return _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.UserId == userId)
                .Where(s => s.Status == SubscriptionStatus.Active
                            || (s.Status == SubscriptionStatus.Trial && s.TrialEndsAt != null && s.TrialEndsAt > now))
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> HasActiveSubscriptionAsync(string userId, CancellationToken ct = default) =>
            await GetActiveAsync(userId, ct) is not null;

        public Task<int> CountUnconsumedPerPostCreditsAsync(string userId, CancellationToken ct = default) =>
            _context.PaymentTransactions.CountAsync(t =>
                t.UserId == userId
                && t.Type == TransactionType.PerPost
                && t.Status == "Succeeded"
                && t.CarListingId == null, ct);

        public async Task<bool> ConsumePerPostCreditAsync(string userId, int carListingId, CancellationToken ct = default)
        {
            var credit = await _context.PaymentTransactions
                .Where(t => t.UserId == userId
                            && t.Type == TransactionType.PerPost
                            && t.Status == "Succeeded"
                            && t.CarListingId == null)
                .OrderBy(t => t.CreatedAt)
                .FirstOrDefaultAsync(ct);
            if (credit is null) return false;

            credit.CarListingId = carListingId;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<(PaymentTransaction tx, bool created)> RecordPaymentIdempotentAsync(
            PaymentTransaction candidate, CancellationToken ct = default)
        {
            if (!string.IsNullOrEmpty(candidate.StripePaymentIntentId))
            {
                var existing = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.StripePaymentIntentId == candidate.StripePaymentIntentId, ct);
                if (existing is not null) return (existing, false);
            }

            _context.PaymentTransactions.Add(candidate);
            try
            {
                await _context.SaveChangesAsync(ct);
                return (candidate, true);
            }
            catch (DbUpdateException)
            {
                // Lost a race against a concurrent webhook delivery — the unique index rejected it.
                _context.Entry(candidate).State = EntityState.Detached;
                var existing = await _context.PaymentTransactions
                    .FirstAsync(t => t.StripePaymentIntentId == candidate.StripePaymentIntentId, ct);
                return (existing, false);
            }
        }

        public Task<List<PaymentTransaction>> GetPaymentHistoryAsync(string userId, CancellationToken ct = default) =>
            _context.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);

        public async Task<decimal> GetLifetimePaidJodAsync(string userId, CancellationToken ct = default) =>
            await _context.PaymentTransactions
                .Where(t => t.UserId == userId && t.Status == "Succeeded")
                .SumAsync(t => (decimal?)t.AmountJod, ct) ?? 0m;

        public async Task<List<int>> GetActiveSubscriberSellerIdsAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;
            var activeUserIds = _context.Subscriptions
                .Where(s => s.Status == SubscriptionStatus.Active
                            || (s.Status == SubscriptionStatus.Trial && s.TrialEndsAt != null && s.TrialEndsAt > now))
                .Select(s => s.UserId);

            return await _context.Sellers
                .Where(se => se.IdentityUserId != null && activeUserIds.Contains(se.IdentityUserId))
                .Select(se => se.Id)
                .ToListAsync(ct);
        }
    }
}
