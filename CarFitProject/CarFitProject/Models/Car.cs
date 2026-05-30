using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class Car
{
    public int Id { get; set; }

    public string Make { get; set; } = null!;

    public string Model { get; set; } = null!;

    public int Year { get; set; }

    public decimal? Price { get; set; }

    public string? Type { get; set; }

    public string? Transmission { get; set; }

    public string? FuelType { get; set; }

    public string? BodyType { get; set; }

    public int? Seats { get; set; }

    public string? FuelEfficiency { get; set; }

    public string? Images { get; set; }

    public int? ScrapedId { get; set; }

    public string? Trim { get; set; }

    public string? Kilometers { get; set; }

    public string? EngineSize { get; set; }

    public string? ExteriorColor { get; set; }

    public string? InteriorColor { get; set; }

    public string? RegionalSpecs { get; set; }

    public string? InteriorOptions { get; set; }

    public string? ExteriorOptions { get; set; }

    public string? TechnologyOptions { get; set; }

    public virtual ICollection<CarListing> CarListings { get; set; } = new List<CarListing>();

    public virtual ICollection<CarImage> CarImages { get; set; } = new List<CarImage>();

    public virtual InspectionReport? InspectionReport { get; set; }

    public virtual ICollection<SavedResult> SavedResults { get; set; } = new List<SavedResult>();
}
