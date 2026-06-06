using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>
    /// 2–3 mechanics seeded per major Jordanian city for the FR-6.4 mechanic
    /// booking flow. Idempotent — only inserts entries whose (Name, City) pair
    /// is not yet present, so admin edits / deletions are preserved.
    /// </summary>
    public static class MechanicSeed
    {
        private static readonly Mechanic[] SeedRows =
        {
            new() { Name = "Amman Auto Care",        City = "Amman",  Phone = "+962-6-555-0101" },
            new() { Name = "Capital Mechanics",      City = "Amman",  Phone = "+962-6-555-0102" },
            new() { Name = "Abdoun Service Garage",  City = "Amman",  Phone = "+962-6-555-0103" },
            new() { Name = "Irbid North Auto",       City = "Irbid",  Phone = "+962-2-555-0201" },
            new() { Name = "Yarmouk Mechanics",      City = "Irbid",  Phone = "+962-2-555-0202" },
            new() { Name = "Zarqa Auto Center",      City = "Zarqa",  Phone = "+962-5-555-0301" },
            new() { Name = "Russeifa Service Hub",   City = "Zarqa",  Phone = "+962-5-555-0302" },
            new() { Name = "Aqaba Coastal Garage",   City = "Aqaba",  Phone = "+962-3-555-0401" },
            new() { Name = "Red Sea Mechanics",      City = "Aqaba",  Phone = "+962-3-555-0402" }
        };

        public static async Task SeedAsync(CarFitDbContext context, CancellationToken ct = default)
        {
            var existing = await context.Mechanics
                .Select(m => new { m.Name, m.City })
                .ToListAsync(ct);
            var existingSet = new HashSet<string>(
                existing.Select(e => Key(e.Name, e.City)),
                StringComparer.OrdinalIgnoreCase);

            var missing = SeedRows
                .Where(r => !existingSet.Contains(Key(r.Name, r.City)))
                .ToList();

            if (missing.Count == 0) return;
            await context.Mechanics.AddRangeAsync(missing, ct);
            await context.SaveChangesAsync(ct);
        }

        private static string Key(string name, string? city) => $"{name}|{city ?? ""}";
    }
}
