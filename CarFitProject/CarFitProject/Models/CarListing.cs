using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class CarListing
{
    public int Id { get; set; }

    public int? CarId { get; set; }

    public int? SellerId { get; set; }

    public decimal? ListingPrice { get; set; }

    public string Status { get; set; } = "Active";

    public bool? InstallmentOption { get; set; }

    public string? PaymentMethodAllowed { get; set; }

    public virtual Car? Car { get; set; }

    public virtual Seller? Seller { get; set; }
}
