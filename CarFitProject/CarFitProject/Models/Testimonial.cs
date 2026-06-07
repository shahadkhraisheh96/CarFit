using System;

namespace CarFitProject.Models;

/// <summary>
/// A review about the CarFit platform overall (not about a specific car or
/// listing). Submitted by signed-in buyers and dealers; hidden from the public
/// home page until an admin sets <see cref="IsApproved"/>.
/// </summary>
public partial class Testimonial
{
    public int Id { get; set; }

    /// <summary>Display name the reviewer chose to show on the card.</summary>
    public string ReviewerName { get; set; } = null!;

    /// <summary>Star rating, 1–5.</summary>
    public int Rating { get; set; }

    public string ReviewText { get; set; } = null!;

    /// <summary>
    /// Identity user id of the submitter. Stored as a plain string — no hard FK
    /// to AspNetUsers because that table lives in ApplicationDbContext (mirrors
    /// the convention used by SearchLog / SavedResult / RecommendationLog).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>False until an admin approves it for public display.</summary>
    public bool IsApproved { get; set; }

    public DateTime CreatedDate { get; set; }
}
