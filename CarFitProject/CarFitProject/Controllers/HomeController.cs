using CarFitProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarFitProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly CarFitDbContext _context;

        public HomeController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // If user isn't logged in, show the public marketing page
            if (!User.Identity.IsAuthenticated)
            {
                var latest = await _context.CarListings
                    .AsNoTracking()
                    .Include(l => l.Car)
                    .Where(l => l.Status == "Active")
                    .OrderByDescending(l => l.Id)
                    .Take(3)
                    .ToListAsync();
                return View(latest);
            }

            // Role Traffic Management Routing Matrix
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            else if (User.IsInRole("Dealer"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Seller" });
            }
            else if (User.IsInRole("Buyer"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Buyer" });
            }
            return View(new List<CarListing>());
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
