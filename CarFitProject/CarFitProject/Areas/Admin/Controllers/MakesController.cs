using CarFitProject.Areas.Admin.Models;
using CarFitProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin CRUD for the <see cref="Make"/> lookup (brand names + logos) that powers
    /// the home page "Browse by Make" grid. Makes are keyed by name to <c>Car.Make</c>,
    /// so the name is read-only once created (renaming here would orphan the join with
    /// existing cars). Logos are set by pasting an image URL (e.g. copied from the
    /// browser); the URL is stored as-is and loaded directly by the &lt;img&gt; tag.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MakesController : Controller
    {
        private readonly CarFitDbContext _context;

        public MakesController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Live approved-listing count per make name (case-insensitive match to Make.Name).
            var counts = await _context.CarListings
                .AsNoTracking()
                .Where(l => l.Status == ListingStatus.Approved && l.Car != null)
                .GroupBy(l => l.Car!.Make)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToListAsync();

            var countByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in counts) countByName[c.Name] = c.Count;

            var rows = (await _context.Makes.AsNoTracking().OrderBy(m => m.Name).ToListAsync())
                .Select(m => new MakeRowViewModel
                {
                    Id = m.Id,
                    Name = m.Name,
                    LogoUrl = m.LogoUrl,
                    CarCount = countByName.TryGetValue(m.Name, out var n) ? n : 0
                })
                .ToList();

            return View(rows);
        }

        // Create (id == 0) or update an existing make. The logo is whatever image URL
        // the admin pasted; an empty value clears it.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(int id, string name, string? logoUrl)
        {
            name = (name ?? string.Empty).Trim();
            var trimmedUrl = (logoUrl ?? string.Empty).Trim();

            if (trimmedUrl.Length > 0)
            {
                if (trimmedUrl.Length > 2048)
                {
                    TempData["ErrorMessage"] = "Logo URL is too long (max 2048 characters).";
                    return RedirectToAction(nameof(Index));
                }
                if (!IsAcceptableLogoUrl(trimmedUrl))
                {
                    TempData["ErrorMessage"] = "Logo URL must be an http(s) link or a site path starting with \"/\".";
                    return RedirectToAction(nameof(Index));
                }
            }

            Make entity;
            if (id == 0)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    TempData["ErrorMessage"] = "Make name is required.";
                    return RedirectToAction(nameof(Index));
                }
                if (name.Length > 50)
                {
                    TempData["ErrorMessage"] = "Make name must be 50 characters or fewer.";
                    return RedirectToAction(nameof(Index));
                }
                if (await _context.Makes.AnyAsync(m => m.Name == name))
                {
                    TempData["ErrorMessage"] = $"A make named \"{name}\" already exists.";
                    return RedirectToAction(nameof(Index));
                }

                entity = new Make { Name = name };
                _context.Makes.Add(entity);
            }
            else
            {
                var existing = await _context.Makes.FirstOrDefaultAsync(m => m.Id == id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Make not found.";
                    return RedirectToAction(nameof(Index));
                }
                entity = existing;
                // Name is intentionally NOT updated on edit — it keeps the Car.Make join intact.
            }

            entity.LogoUrl = trimmedUrl.Length == 0 ? null : trimmedUrl;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = id == 0 ? $"Added make \"{entity.Name}\"." : $"Updated make \"{entity.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Makes.FirstOrDefaultAsync(m => m.Id == id);
            if (entity == null)
            {
                TempData["ErrorMessage"] = "Make not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Makes.Remove(entity);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Deleted make \"{entity.Name}\".";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Accepts absolute http/https URLs or site-relative paths ("/..."); rejects anything else (e.g. javascript:).</summary>
        private static bool IsAcceptableLogoUrl(string url)
        {
            if (url.StartsWith("/")) return true;
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
