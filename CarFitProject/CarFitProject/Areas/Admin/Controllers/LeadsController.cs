using CarFitProject.Data;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin lead report: every "Buy This Car" buyer-contact intent, newest first. Intents live in
    /// CarFitDbContext; buyer/seller names come from ApplicationDbContext, so the page is enriched
    /// from Identity in a second pass (same pattern as the subscriptions report).
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LeadsController : Controller
    {
        private const int PageSize = 20;

        private readonly CarFitDbContext _context;
        private readonly ApplicationDbContext _identity;

        public LeadsController(CarFitDbContext context, ApplicationDbContext identity)
        {
            _context = context;
            _identity = identity;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var pageIntents = await PaginatedList<BuyerContactIntent>.CreateAsync(
                _context.BuyerContactIntents.AsNoTracking().OrderByDescending(x => x.CreatedAt),
                page, PageSize);

            var listingIds = pageIntents.Select(p => p.ListingId).Distinct().ToList();
            var listings = await _context.CarListings.AsNoTracking()
                .Where(l => listingIds.Contains(l.Id))
                .Include(l => l.Car)
                .ToDictionaryAsync(l => l.Id);

            var userIds = pageIntents
                .SelectMany(p => new[] { p.BuyerUserId, p.SellerUserId })
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct()
                .ToList()!;
            var users = await _identity.Users
                .Where(u => userIds.Contains(u.Id))
                .Select(u => new { u.Id, u.FullName, u.Email })
                .ToDictionaryAsync(u => u.Id);

            string? NameOf(string? id) =>
                !string.IsNullOrEmpty(id) && users.TryGetValue(id, out var u) ? (u.FullName ?? u.Email ?? id) : id;

            var rows = pageIntents.Select(p => new AdminLeadRow
            {
                Id = p.Id,
                ListingId = p.ListingId,
                CarLabel = listings.TryGetValue(p.ListingId, out var l) && l.Car != null
                    ? $"{l.Car.Make} {l.Car.Model} {l.Car.Year}" : null,
                BuyerName = NameOf(p.BuyerUserId) ?? p.BuyerUserId,
                SellerName = NameOf(p.SellerUserId),
                CreatedAt = p.CreatedAt
            }).ToList();

            var vm = new AdminLeadsViewModel
            {
                Rows = new PaginatedList<AdminLeadRow>(rows, pageIntents.TotalCount, pageIntents.PageIndex, PageSize)
            };
            return View(vm);
        }
    }
}
