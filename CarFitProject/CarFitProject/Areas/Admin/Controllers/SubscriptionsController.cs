using CarFitProject.Data;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin report of every subscription: subscriber, role, plan/fee, status, dates, lifetime paid.
    /// Filterable by status and paginated. Subscriptions live in CarFitDbContext while user names
    /// live in ApplicationDbContext, so the page of rows is enriched from Identity in a second pass.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SubscriptionsController : Controller
    {
        private const int PageSize = 20;

        private readonly CarFitDbContext _context;
        private readonly ApplicationDbContext _identity;

        public SubscriptionsController(CarFitDbContext context, ApplicationDbContext identity)
        {
            _context = context;
            _identity = identity;
        }

        public async Task<IActionResult> Index(string? status = null, int page = 1)
        {
            var query = _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<SubscriptionStatus>(status, out var parsed))
            {
                query = query.Where(s => s.Status == parsed);
            }

            query = query.OrderByDescending(s => s.CreatedAt);

            var pageSubs = await PaginatedList<Subscription>.CreateAsync(query, page, PageSize);

            var userIds = pageSubs.Select(s => s.UserId).Distinct().ToList();

            // Enrich from Identity (names/emails) and roll up lifetime paid per user — both keyed by userId.
            var users = await _identity.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToDictionaryAsync(u => u.Id);

            var lifetime = await _context.PaymentTransactions
                .Where(t => userIds.Contains(t.UserId) && t.Status == "Succeeded")
                .GroupBy(t => t.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(t => t.AmountJod) })
                .ToDictionaryAsync(x => x.UserId, x => x.Total);

            var rows = pageSubs.Select(s => new AdminSubscriptionRow
            {
                UserName = users.TryGetValue(s.UserId, out var u) ? (u.FullName ?? u.Email ?? s.UserId) : s.UserId,
                Email = users.TryGetValue(s.UserId, out var u2) ? u2.Email : null,
                Role = s.Plan?.TargetRole ?? "",
                PlanName = s.Plan?.Name ?? "",
                FeeJod = s.Plan?.AmountJod ?? 0m,
                Interval = s.Plan?.BillingInterval ?? PlanBillingInterval.OneTime,
                Status = s.Status,
                StartDate = s.CurrentPeriodStart ?? s.CreatedAt,
                NextBilling = s.CurrentPeriodEnd,
                LifetimePaidJod = lifetime.TryGetValue(s.UserId, out var total) ? total : 0m
            }).ToList();

            var vm = new AdminSubscriptionsViewModel
            {
                Rows = new PaginatedList<AdminSubscriptionRow>(rows, pageSubs.TotalCount, pageSubs.PageIndex, PageSize),
                StatusFilter = status
            };
            return View(vm);
        }
    }
}
