using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class CarListing
{
    public int Id { get; set; }

    public int? CarId { get; set; }

    public int? SellerId { get; set; }

    public decimal? ListingPrice { get; set; }

    public string Status { get; set; } = ListingStatus.Draft;

    /// <summary>Free-text reason an admin gave when rejecting the listing (Phase 10). Shown to the seller.</summary>
    public string? RejectionReason { get; set; }

    public bool? InstallmentOption { get; set; }

    public string? PaymentMethodAllowed { get; set; }

    /// <summary>When the listing was created. Phase 9.1 — drives admin inventory ordering + the Created column.</summary>
    public DateTime CreatedAt { get; set; }

    public virtual Car? Car { get; set; }

    public virtual Seller? Seller { get; set; }
}
