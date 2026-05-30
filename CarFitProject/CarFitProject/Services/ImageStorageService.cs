using CarFitProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CarFitProject.Services
{
    /// <summary>
    /// Listing-image pipeline (FR-3.4, NFR-P4). Saves uploaded files as
    /// WebP under wwwroot/uploads/cars/{carId}/ and records <see cref="CarImage"/> rows.
    /// </summary>
    public interface IImageStorageService
    {
        /// <summary>
        /// Converts each uploaded image to WebP (≤ 200 KB, longest edge ≤ 1600 px) and
        /// saves it to wwwroot/uploads/cars/{carId}/. Persists CarImage rows with
        /// SortOrder and IsPrimary. Returns the persisted CarImage list.
        /// </summary>
        Task<List<CarImage>> SaveImagesAsync(int carId, IEnumerable<IFormFile> files, int startSortOrder, bool makeFirstPrimary);

        /// <summary>Deletes one image record + its file on disk.</summary>
        Task<bool> DeleteAsync(int imageId);
    }

    public class ImageStorageService : IImageStorageService
    {
        private const long MaxBytes = 200 * 1024;
        private const int MaxLongEdgePx = 1600;
        private const int MinQuality = 30;
        private const int StartingQuality = 85;
        private const int QualityStep = 10;

        private readonly CarFitDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ImageStorageService(CarFitDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<List<CarImage>> SaveImagesAsync(int carId, IEnumerable<IFormFile> files, int startSortOrder, bool makeFirstPrimary)
        {
            var fileList = files?.ToList() ?? new List<IFormFile>();
            if (fileList.Count == 0) return new List<CarImage>();

            var carDir = Path.Combine(_env.WebRootPath, "uploads", "cars", carId.ToString());
            Directory.CreateDirectory(carDir);

            var saved = new List<CarImage>();
            var sort = startSortOrder;
            var first = true;

            foreach (var file in fileList)
            {
                if (file.Length == 0) continue;

                using var input = file.OpenReadStream();
                using var image = await Image.LoadAsync(input);

                var longEdge = Math.Max(image.Width, image.Height);
                if (longEdge > MaxLongEdgePx)
                {
                    var ratio = (double)MaxLongEdgePx / longEdge;
                    image.Mutate(x => x.Resize(
                        (int)(image.Width * ratio),
                        (int)(image.Height * ratio)));
                }

                var fileName = $"{Guid.NewGuid():N}.webp";
                var fullPath = Path.Combine(carDir, fileName);

                var quality = StartingQuality;
                byte[] payload;
                while (true)
                {
                    using var ms = new MemoryStream();
                    await image.SaveAsync(ms, new WebpEncoder { Quality = quality });
                    payload = ms.ToArray();
                    if (payload.LongLength <= MaxBytes || quality <= MinQuality) break;
                    quality -= QualityStep;
                }

                await File.WriteAllBytesAsync(fullPath, payload);

                var carImage = new CarImage
                {
                    CarId = carId,
                    Url = $"/uploads/cars/{carId}/{fileName}",
                    SortOrder = sort++,
                    IsPrimary = makeFirstPrimary && first
                };
                _context.CarImages.Add(carImage);
                saved.Add(carImage);
                first = false;
            }

            if (saved.Count > 0) await _context.SaveChangesAsync();
            return saved;
        }

        public async Task<bool> DeleteAsync(int imageId)
        {
            var image = await _context.CarImages.FirstOrDefaultAsync(i => i.Id == imageId);
            if (image == null) return false;

            var webPath = image.Url?.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (!string.IsNullOrEmpty(webPath))
            {
                var fullPath = Path.Combine(_env.WebRootPath, webPath);
                if (File.Exists(fullPath))
                {
                    try { File.Delete(fullPath); } catch { /* leave orphan file rather than block */ }
                }
            }

            _context.CarImages.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
