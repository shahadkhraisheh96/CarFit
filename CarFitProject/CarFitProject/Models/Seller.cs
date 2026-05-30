using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class Seller
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Type { get; set; }

    public string? City { get; set; }

    public string? Neighborhood { get; set; }

    public string? IdentityUserId { get; set; }

    public string? Email { get; set; }

    public bool IsApproved { get; set; }

    public string? Tier { get; set; }

    public virtual ICollection<CarListing> CarListings { get; set; } = new List<CarListing>();
}
