using CarFitProject.Models;
using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarFitProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly CarFitDbContext _context;
        private readonly ITestimonialService _testimonials;

        public HomeController(CarFitDbContext context, ITestimonialService testimonials)
        {
            _context = context;
            _testimonials = testimonials;
        }

        public async Task<IActionResult> Index()
        {
            var latest = await _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Where(l => l.Status == ListingStatus.Approved)
                .OrderByDescending(l => l.Id)
                .Take(3)
                .ToListAsync();

            // "Browse by Make" grid: distinct makes that have at least one approved
            // listing, with a live available-car count. Ordered by inventory depth
            // (most cars first) so the brands buyers are most likely to find float to
            // the top, then alphabetically as a tiebreaker. Read-only → AsNoTracking.
            // Grouping over approved listings inherently excludes makes with zero
            // available cars.
            var makeCounts = await _context.CarListings
                .AsNoTracking()
                .Where(l => l.Status == ListingStatus.Approved && l.Car != null)
                .GroupBy(l => l.Car!.Make)
                .Select(g => new { Name = g.Key, CarCount = g.Count() })
                .ToListAsync();

            // Brand logos come from the Makes lookup, keyed by name (case-insensitive).
            // A missing row → null LogoUrl → the view shows an initial-letter badge.
            var logoByMake = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var m in await _context.Makes.AsNoTracking().ToListAsync())
            {
                logoByMake[m.Name] = m.LogoUrl;
            }

            ViewBag.Makes = makeCounts
                .Select(x => new MakeSummaryViewModel
                {
                    Name = x.Name,
                    CarCount = x.CarCount,
                    LogoUrl = logoByMake.TryGetValue(x.Name, out var url) ? url : null
                })
                .OrderByDescending(m => m.CarCount)
                .ThenBy(m => m.Name)
                .ToList();

            // Approved website reviews for the home-page carousel.
            ViewBag.Testimonials = await _testimonials.GetApprovedAsync();
            return View(latest);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactFormViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(ContactFormViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var message = new ContactMessage
            {
                Name = vm.Name,
                Email = vm.Email,
                Subject = vm.Subject,
                Message = vm.Message,
                CreatedAt = DateTime.UtcNow,
                Status = "New"
            };

            _context.ContactMessages.Add(message);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you! Your message has been sent successfully. We will get back to you shortly.";
            return RedirectToAction(nameof(Contact));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
