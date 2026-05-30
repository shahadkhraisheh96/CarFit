using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class UserProfile
{
    public int ProfileId { get; set; }

    public string UserId { get; set; } = null!;

    public string ProfileName { get; set; } = null!;

    public int? Age { get; set; }

    public string? MaritalStatus { get; set; }

    public bool? HasKids { get; set; }

    public int? KidsCount { get; set; }

    public string? Purpose { get; set; }

    public decimal? BudgetMin { get; set; }

    public decimal BudgetMax { get; set; }

    public string? PaymentMethod { get; set; }

    public string TransmissionPref { get; set; } = null!;

    public string SizePref { get; set; } = null!;

    public string? TripType { get; set; }

    public string? ConditionPref { get; set; }

    public int? InstallmentMonths { get; set; }

    public bool? IsActive { get; set; }
}
