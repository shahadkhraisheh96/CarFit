using System;

namespace CarFitProject.Models;

/// <summary>
/// Raw inspection evidence uploaded by the seller when listing a Used car
/// (Phase 10). These are the original scanned documents (PDF/JPG/PNG/WebP) the
/// seller provides as proof — distinct from <see cref="InspectionReport"/>, which
/// is the structured report an admin fills in. Both link to the same <see cref="Car"/>.
/// New cars are allowed zero uploads; Used cars require at least one.
/// </summary>
public partial class SellerInspectionUpload
{
    public int Id { get; set; }

    public int CarId { get; set; }

    /// <summary>Original (sanitized) file name, for display only — never used as the stored path.</summary>
    public string FileName { get; set; } = null!;

    /// <summary>Web-relative path under wwwroot, e.g. /uploads/inspections/{carId}/{guid}.pdf.</summary>
    public string StoredPath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAtUtc { get; set; }

    /// <summary>Identity user id of the seller who uploaded the file.</summary>
    public string? UploadedByUserId { get; set; }

    public virtual Car Car { get; set; } = null!;
}
