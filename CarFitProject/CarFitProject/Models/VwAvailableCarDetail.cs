using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class VwAvailableCarDetail
{
    public int CarId { get; set; }

    public int? ScrapedId { get; set; }

    public string Make { get; set; } = null!;

    public string Model { get; set; } = null!;

    public string? Trim { get; set; }

    public int Year { get; set; }

    public string? Kilometers { get; set; }

    public string? BodyType { get; set; }

    public int? Seats { get; set; }

    public string? FuelType { get; set; }

    public string? Transmission { get; set; }

    public string? EngineSize { get; set; }

    public string? ExteriorColor { get; set; }

    public string? InteriorColor { get; set; }

    public string? RegionalSpecs { get; set; }

    public decimal? Price { get; set; }

    public string? InteriorOptions { get; set; }

    public string? ExteriorOptions { get; set; }

    public string? TechnologyOptions { get; set; }

    public string? Images { get; set; }

    public int ListingId { get; set; }

    public decimal? ListingPrice { get; set; }

    public string? PaymentMethodAllowed { get; set; }

    public int SellerId { get; set; }

    public string SellerName { get; set; } = null!;

    public string? City { get; set; }

    public string? Neighborhood { get; set; }

    public string? BodyCondition { get; set; }

    public string? PaintStatus { get; set; }

    public string? DescriptionScore { get; set; }

    public decimal TrustScore { get; set; }
}
