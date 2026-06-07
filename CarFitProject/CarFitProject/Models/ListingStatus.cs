namespace CarFitProject.Models;

/// <summary>
/// Canonical lifecycle states for a <see cref="CarListing"/> (Phase 10 approval flow).
/// Stored as the string value in <c>CarListings.status</c>.
///
/// Lifecycle: a seller submits a listing as <see cref="PendingInspectionReview"/>
/// (both New and Used). An admin reviews it and either <see cref="Approved"/>
/// (publicly visible) or <see cref="Rejected"/> (reason stored on the listing).
/// A Used car can only be approved once a structured inspection report exists.
/// <see cref="Sold"/> is a terminal post-approval state set by the seller's
/// "mark sold" action. <see cref="Draft"/> is the column default and is not yet
/// produced by any flow (reserved for a future save-as-draft).
/// </summary>
public static class ListingStatus
{
    public const string Draft = "Draft";
    public const string PendingInspectionReview = "PendingInspectionReview";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Sold = "Sold";

    /// <summary>Statuses that are publicly visible in search / detail / recommendations.</summary>
    public static readonly string[] PublicStatuses = { Approved };
}
