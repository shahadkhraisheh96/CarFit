using System;

namespace CarFitProject.Models;

public partial class SearchLog
{
    public int Id { get; set; }

    public string? Term { get; set; }

    public string? FiltersJson { get; set; }

    /// <summary>
    /// Identity user id of the searcher when signed in; null for anonymous
    /// searches. Stored as plain string — no hard FK to AspNetUsers because
    /// that table lives in ApplicationDbContext.
    /// </summary>
    public string? UserId { get; set; }

    public DateTime CreatedAt { get; set; }
}
