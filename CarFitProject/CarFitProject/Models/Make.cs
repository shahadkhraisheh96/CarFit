namespace CarFitProject.Models;

/// <summary>
/// Lookup table for car makes (brands), keyed by <see cref="Name"/> which matches
/// the free-text <c>Car.Make</c> string used throughout the app. This is a thin
/// reference table — it does NOT replace <c>Car.Make</c> with a foreign key — so it
/// can carry per-brand metadata (currently a logo) without touching the Car schema
/// or the existing make-filter/search/recommendation paths.
/// </summary>
public partial class Make
{
    public int Id { get; set; }

    /// <summary>Brand name; matches <c>Car.Make</c> (e.g. "Toyota", "Mercedes-Benz").</summary>
    public string Name { get; set; } = null!;

    /// <summary>Web-relative path to the brand logo, e.g. /img/makes/toyota.png. Null → the UI shows an initial-letter badge.</summary>
    public string? LogoUrl { get; set; }
}
