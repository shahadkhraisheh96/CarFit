using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DealersController : Controller
    {
        private static readonly string[] AllowedTiers = { "Basic", "Standard", "Premium" };

        private readonly CarFitDbContext _context;

        public DealersController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Pending()
        {
            var pending = await _context.Sellers
                .Where(s => !s.IsApproved)
                .OrderBy(s => s.Id)
                .ToListAsync();

            ViewBag.Tiers = AllowedTiers;
            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string tier)
        {
            if (!AllowedTiers.Contains(tier))
            {
                TempData["ErrorMessage"] = "Please pick a tier (Basic, Standard, or Premium).";
                return RedirectToAction(nameof(Pending));
            }

            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                TempData["ErrorMessage"] = "That dealer no longer exists.";
                return RedirectToAction(nameof(Pending));
            }

            seller.IsApproved = true;
            seller.Tier = tier;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Approved {seller.Name} at the {tier} tier.";
            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                TempData["ErrorMessage"] = "That dealer no longer exists.";
                return RedirectToAction(nameof(Pending));
            }

            var hasListings = await _context.CarListings.AnyAsync(l => l.SellerId == seller.Id);
            if (hasListings)
            {
                TempData["ErrorMessage"] = $"Cannot reject {seller.Name}: they already have listings. Delete or reassign listings first.";
                return RedirectToAction(nameof(Pending));
            }

            _context.Sellers.Remove(seller);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Rejected and removed dealer application: {seller.Name}.";
            return RedirectToAction(nameof(Pending));
        }

        // --- NEW CRUD ACTIONS ---
        
        public async Task<IActionResult> Index()
        {
            var sellers = await _context.Sellers
                .AsNoTracking()
                .OrderBy(s => s.Type == "Dealership" ? 0 : 1)
                .ThenBy(s => s.Name)
                .ToListAsync();
            return View(sellers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var seller = await _context.Sellers
                .Include(s => s.CarListings)
                    .ThenInclude(l => l.Car)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (seller == null) return NotFound();
            return View(seller);
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Tiers = AllowedTiers;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarFitProject.Models.Seller model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Tiers = AllowedTiers;
                return View(model);
            }

            model.IsApproved = true; // Admins creating sellers are automatically approved
            
            _context.Sellers.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully created dealership/seller: {model.Name}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var seller = await _context.Sellers.FindAsync(id);
            if (seller == null)
            {
                TempData["ErrorMessage"] = "Seller not found.";
                return RedirectToAction(nameof(Index));
            }

            var hasListings = await _context.CarListings.AnyAsync(l => l.SellerId == seller.Id);
            if (hasListings)
            {
                TempData["ErrorMessage"] = $"Cannot delete {seller.Name}: they have associated car listings. Please remove their listings first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Sellers.Remove(seller);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Seller {seller.Name} has been deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
