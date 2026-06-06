using System.Linq;
using System.Threading.Tasks;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class DealershipsController : Controller
    {
        private readonly CarFitDbContext _context;

        public DealershipsController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var sellers = await _context.Sellers
                .AsNoTracking()
                .Where(s => s.IsApproved && (
                    s.Type == "Dealership" || 
                    s.Type == "Seller" || 
                    s.CarListings.Any(l => l.Status == ListingStatus.Approved)
                ))
                .Select(s => new DealershipViewModel
                {
                    Id = s.Id,
                    Name = s.Name,
                    City = s.City,
                    Type = s.Type,
                    ActiveListingsCount = s.CarListings.Count(l => l.Status == ListingStatus.Approved)
                })
                .ToListAsync();

            var orderedSellers = sellers
                .OrderByDescending(s => string.Equals(s.Type, "Dealership", System.StringComparison.OrdinalIgnoreCase))
                .ThenByDescending(s => s.ActiveListingsCount)
                .ThenBy(s => s.Name)
                .ToList();

            return View(orderedSellers);
        }
    }
}
