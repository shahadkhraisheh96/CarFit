using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private const int TopRecommendedN = 5;
        private const int TopSearchTermsN = 10;
        private const int MonthsBack = 11;

        private readonly CarFitDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(CarFitDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // FR-7.5: real analytics dashboard.
        public async Task<IActionResult> Index()
        {
            var vm = new AdminAnalyticsViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalActiveListings = await _context.CarListings.CountAsync(l => l.Status == "Active"),
                TotalGlossaryTerms = await _context.InspectionTermsGlossaries.CountAsync(),
                TopRecommended = await ComputeTopRecommendedAsync(),
                TopSearchTerms = await ComputeTopSearchTermsAsync(),
                RegistrationsByMonth = await ComputeRegistrationsByMonthAsync(),
                RecentListings = await _context.CarListings
                    .AsNoTracking()
                    .Include(l => l.Car)
                    .OrderByDescending(l => l.Id)
                    .Take(5)
                    .ToListAsync()
            };

            return View(vm);
        }

        private async Task<List<TopRecommendedCarRow>> ComputeTopRecommendedAsync()
        {
            // RecommendedCarIds is a CSV. Pull just the column, split + tally in
            // memory — small set (top-5 of all logs) and SQL Server's STRING_SPLIT
            // isn't worth the EF gymnastics here.
            var csvs = await _context.RecommendationLogs
                .Where(r => r.RecommendedCarIds != null && r.RecommendedCarIds != "")
                .Select(r => r.RecommendedCarIds!)
                .ToListAsync();

            var counts = csvs
                .SelectMany(s => s.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(t => int.TryParse(t.Trim(), out var id) ? id : 0)
                .Where(id => id > 0)
                .GroupBy(id => id)
                .Select(g => new { CarId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(TopRecommendedN)
                .ToList();

            if (counts.Count == 0) return new List<TopRecommendedCarRow>();

            var topIds = counts.Select(c => c.CarId).ToList();
            var carsLookup = await _context.Cars
                .AsNoTracking()
                .Where(c => topIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Make, c.Model, c.Year })
                .ToDictionaryAsync(c => c.Id);

            return counts
                .Select(c => new TopRecommendedCarRow(
                    c.CarId,
                    carsLookup.TryGetValue(c.CarId, out var car)
                        ? $"{car.Make} {car.Model} {car.Year}".Trim()
                        : $"Car #{c.CarId}",
                    c.Count))
                .ToList();
        }

        private async Task<List<TopSearchTermRow>> ComputeTopSearchTermsAsync()
        {
            var rows = await _context.SearchLogs
                .AsNoTracking()
                .Where(s => s.Term != null && s.Term != "")
                .GroupBy(s => s.Term)
                .Select(g => new { Term = g.Key!, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ThenBy(x => x.Term)
                .Take(TopSearchTermsN)
                .ToListAsync();

            return rows.Select(r => new TopSearchTermRow(r.Term, r.Count)).ToList();
        }

        private async Task<List<RegistrationsByMonthRow>> ComputeRegistrationsByMonthAsync()
        {
            var today = DateTime.UtcNow;
            var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-MonthsBack);

            var rawCounts = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.CreatedAt >= firstMonth)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .ToListAsync();

            var byKey = rawCounts.ToDictionary(r => (r.Year, r.Month), r => r.Count);

            var series = new List<RegistrationsByMonthRow>(MonthsBack + 1);
            for (int i = 0; i <= MonthsBack; i++)
            {
                var d = firstMonth.AddMonths(i);
                byKey.TryGetValue((d.Year, d.Month), out var count);
                series.Add(new RegistrationsByMonthRow(d.Year, d.Month, count));
            }
            return series;
        }
    }
}
