using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>
    /// Ensures the <see cref="Make"/> lookup has a row for every distinct
    /// <c>Car.Make</c> present in the catalog. New rows start with no logo — an admin
    /// sets each logo by pasting an image URL on the Manage Makes page, and the UI
    /// shows an initial-letter badge until then. Idempotent — only inserts makes not
    /// already present, so admin-set logos are preserved.
    /// </summary>
    public static class MakeSeed
    {
        public static async Task SeedAsync(CarFitDbContext context, CancellationToken ct = default)
        {
            var distinctMakes = await context.Cars
                .AsNoTracking()
                .Select(c => c.Make)
                .Distinct()
                .ToListAsync(ct);

            var existing = new HashSet<string>(
                await context.Makes.Select(m => m.Name).ToListAsync(ct),
                StringComparer.OrdinalIgnoreCase);

            var missing = distinctMakes
                .Where(name => !string.IsNullOrWhiteSpace(name) && !existing.Contains(name))
                .Select(name => new Make { Name = name }) // LogoUrl left null; admin pastes a URL later
                .ToList();

            if (missing.Count == 0) return;
            await context.Makes.AddRangeAsync(missing, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
