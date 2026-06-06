using CarFitProject.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>Outcome of validating + storing a batch of seller inspection files.</summary>
    public record InspectionUploadResult(bool Ok, string Message, List<SellerInspectionUpload> Saved);

    /// <summary>
    /// Stores the raw inspection evidence a seller uploads for a Used car (Phase 10).
    /// Files are validated server-side (count, per-file size, content-type AND magic
    /// bytes) and written AS-IS — PDFs are never run through the WebP image pipeline —
    /// under wwwroot/uploads/inspections/{carId}/ with safe, unique GUID file names.
    /// </summary>
    public interface IInspectionUploadService
    {
        int MaxFiles { get; }
        long MaxBytesPerFile { get; }

        /// <summary>
        /// Validates and persists the batch. On any validation failure NOTHING is written
        /// (all-or-nothing) and <see cref="InspectionUploadResult.Ok"/> is false with a
        /// user-facing message. Pass an empty/null list to no-op successfully.
        /// </summary>
        Task<InspectionUploadResult> SaveUploadsAsync(int carId, IEnumerable<IFormFile>? files, string? userId);

        Task<List<SellerInspectionUpload>> GetForCarAsync(int carId);

        Task<bool> DeleteAsync(int uploadId);
    }

    public class InspectionUploadService : IInspectionUploadService
    {
        public int MaxFiles => 10;
        public long MaxBytesPerFile => 25L * 1024 * 1024; // 25 MB

        private readonly CarFitDbContext _context;
        private readonly IWebHostEnvironment _env;

        public InspectionUploadService(CarFitDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Allowed (content-type, extension) pairs. Content-type is checked AND the
        // first bytes are sniffed below, so a renamed/forged file is rejected.
        private static readonly Dictionary<string, string> AllowedExtByContentType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["application/pdf"] = ".pdf",
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp",
        };

        public async Task<InspectionUploadResult> SaveUploadsAsync(int carId, IEnumerable<IFormFile>? files, string? userId)
        {
            var list = (files ?? Enumerable.Empty<IFormFile>()).Where(f => f.Length > 0).ToList();
            if (list.Count == 0) return new InspectionUploadResult(true, "No files.", new());

            if (list.Count > MaxFiles)
                return new InspectionUploadResult(false, $"You can upload at most {MaxFiles} inspection files.", new());

            // Validate everything BEFORE writing anything to disk (all-or-nothing).
            foreach (var file in list)
            {
                if (file.Length > MaxBytesPerFile)
                    return new InspectionUploadResult(false,
                        $"\"{file.FileName}\" is too large. Each file must be {MaxBytesPerFile / (1024 * 1024)} MB or less.", new());

                if (!AllowedExtByContentType.ContainsKey(file.ContentType))
                    return new InspectionUploadResult(false,
                        $"\"{file.FileName}\" has an unsupported type. Allowed: PDF, JPG, PNG, WebP.", new());

                if (!await SniffMatchesAsync(file))
                    return new InspectionUploadResult(false,
                        $"\"{file.FileName}\" doesn't appear to be a valid {file.ContentType} file.", new());
            }

            var carDir = Path.Combine(_env.WebRootPath, "uploads", "inspections", carId.ToString());
            Directory.CreateDirectory(carDir);

            var saved = new List<SellerInspectionUpload>();
            foreach (var file in list)
            {
                var ext = AllowedExtByContentType[file.ContentType];
                var storedName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(carDir, storedName);

                using (var fs = new FileStream(fullPath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(fs); // stored AS-IS, no re-encoding
                }

                var row = new SellerInspectionUpload
                {
                    CarId = carId,
                    FileName = SanitizeDisplayName(file.FileName),
                    StoredPath = $"/uploads/inspections/{carId}/{storedName}",
                    ContentType = file.ContentType,
                    FileSizeBytes = file.Length,
                    UploadedAtUtc = DateTime.UtcNow,
                    UploadedByUserId = userId
                };
                _context.SellerInspectionUploads.Add(row);
                saved.Add(row);
            }

            await _context.SaveChangesAsync();
            return new InspectionUploadResult(true, $"{saved.Count} inspection file(s) uploaded.", saved);
        }

        public Task<List<SellerInspectionUpload>> GetForCarAsync(int carId)
            => _context.SellerInspectionUploads
                .AsNoTracking()
                .Where(u => u.CarId == carId)
                .OrderBy(u => u.UploadedAtUtc)
                .ToListAsync();

        public async Task<bool> DeleteAsync(int uploadId)
        {
            var upload = await _context.SellerInspectionUploads.FirstOrDefaultAsync(u => u.Id == uploadId);
            if (upload == null) return false;

            var rel = upload.StoredPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, rel);
            if (File.Exists(fullPath))
            {
                try { File.Delete(fullPath); } catch { /* leave orphan file rather than block the delete */ }
            }

            _context.SellerInspectionUploads.Remove(upload);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>Reads the leading bytes and confirms they match the declared content-type's signature.</summary>
        private static async Task<bool> SniffMatchesAsync(IFormFile file)
        {
            var header = new byte[12];
            await using (var stream = file.OpenReadStream())
            {
                var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
                if (read < 4) return false;
            }

            return file.ContentType.ToLowerInvariant() switch
            {
                "application/pdf" => Starts(header, 0x25, 0x50, 0x44, 0x46),               // %PDF
                "image/jpeg" => Starts(header, 0xFF, 0xD8, 0xFF),                          // JPEG SOI
                "image/png" => Starts(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A), // PNG
                "image/webp" => Starts(header, 0x52, 0x49, 0x46, 0x46)                     // RIFF
                                && header[8] == 0x57 && header[9] == 0x45                  // "WE"
                                && header[10] == 0x42 && header[11] == 0x50,               // "BP"
                _ => false
            };
        }

        private static bool Starts(byte[] data, params byte[] prefix)
        {
            if (data.Length < prefix.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i]) return false;
            return true;
        }

        /// <summary>Keeps only the file's base name (no path) and strips anything unsafe for display.</summary>
        private static string SanitizeDisplayName(string original)
        {
            var name = Path.GetFileName(original ?? string.Empty);
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim();
            if (string.IsNullOrEmpty(name)) name = "inspection";
            return name.Length > 255 ? name[..255] : name;
        }
    }
}
