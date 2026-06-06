using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class RecommendationLog
{
    public int Id { get; set; }

    public string? UserId { get; set; }

    public string? RecommendedCarIds { get; set; }

    public decimal? Score { get; set; }

    public DateTime? CreatedAt { get; set; }
}
