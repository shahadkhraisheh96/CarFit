using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CarFitProject.Models;

namespace CarFitProject.Services
{
    public static class CarFitDataSeed
    {
        private class SeedContainer
        {
            public string? Cars { get; set; }
            public string? Listings { get; set; }
            public string? Sellers { get; set; }
            public string? Makes { get; set; }
            public string? Reports { get; set; }
            public string? Images { get; set; }
        }

        public static async Task SeedAsync(CarFitDbContext context, string contentRootPath, ILogger logger, CancellationToken ct = default)
        {
            try
            {
                if (await context.Cars.AnyAsync(ct))
                {
                    logger.LogInformation("Car catalog already populated. Skipping CarFitDataSeed.");
                    return;
                }

                var seedFile = Path.Combine(contentRootPath, "carfit_seed_data.json");
                if (!File.Exists(seedFile))
                {
                    seedFile = Path.Combine(AppContext.BaseDirectory, "carfit_seed_data.json");
                }

                if (!File.Exists(seedFile))
                {
                    logger.LogWarning("carfit_seed_data.json not found at {Path}", seedFile);
                    return;
                }

                var jsonText = await File.ReadAllTextAsync(seedFile, ct);
                var container = JsonSerializer.Deserialize<SeedContainer>(jsonText);
                if (container == null) return;

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // 1. Makes
                if (!string.IsNullOrWhiteSpace(container.Makes))
                {
                    var makes = JsonSerializer.Deserialize<List<Make>>(container.Makes, jsonOptions);
                    if (makes != null)
                    {
                        foreach (var m in makes)
                        {
                            if (!await context.Makes.AnyAsync(x => x.Id == m.Id || x.Name == m.Name, ct))
                            {
                                context.Makes.Add(m);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                // 2. Sellers
                if (!string.IsNullOrWhiteSpace(container.Sellers))
                {
                    var sellers = JsonSerializer.Deserialize<List<Seller>>(container.Sellers, jsonOptions);
                    if (sellers != null)
                    {
                        foreach (var s in sellers)
                        {
                            if (!await context.Sellers.AnyAsync(x => x.Id == s.Id, ct))
                            {
                                context.Sellers.Add(s);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                // 3. Cars
                if (!string.IsNullOrWhiteSpace(container.Cars))
                {
                    var cars = JsonSerializer.Deserialize<List<Car>>(container.Cars, jsonOptions);
                    if (cars != null)
                    {
                        foreach (var c in cars)
                        {
                            if (!await context.Cars.AnyAsync(x => x.Id == c.Id, ct))
                            {
                                context.Cars.Add(c);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                // 4. Car Listings
                if (!string.IsNullOrWhiteSpace(container.Listings))
                {
                    var listings = JsonSerializer.Deserialize<List<CarListing>>(container.Listings, jsonOptions);
                    if (listings != null)
                    {
                        foreach (var l in listings)
                        {
                            if (!await context.CarListings.AnyAsync(x => x.Id == l.Id, ct))
                            {
                                context.CarListings.Add(l);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                // 5. Car Images
                if (!string.IsNullOrWhiteSpace(container.Images))
                {
                    var images = JsonSerializer.Deserialize<List<CarImage>>(container.Images, jsonOptions);
                    if (images != null)
                    {
                        foreach (var img in images)
                        {
                            if (!await context.CarImages.AnyAsync(x => x.Id == img.Id, ct))
                            {
                                context.CarImages.Add(img);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                // 6. Inspection Reports
                if (!string.IsNullOrWhiteSpace(container.Reports))
                {
                    var reports = JsonSerializer.Deserialize<List<InspectionReport>>(container.Reports, jsonOptions);
                    if (reports != null)
                    {
                        foreach (var r in reports)
                        {
                            if (!await context.InspectionReports.AnyAsync(x => x.CarId == r.CarId, ct))
                            {
                                context.InspectionReports.Add(r);
                            }
                        }
                        await context.SaveChangesAsync(ct);
                    }
                }

                logger.LogInformation("Successfully seeded catalog from carfit_seed_data.json!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while seeding CarFit catalog data.");
            }
        }
    }
}
