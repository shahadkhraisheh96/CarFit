using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    /// <summary>
    /// Composite view model for the admin Phase-10 review page. Bundles the listing
    /// (car specs, photos, seller), the raw seller-uploaded inspection evidence, and
    /// the structured inspection form (reusing <see cref="InspectionReportFormViewModel"/>)
    /// so the admin can review everything and fill the report on one page.
    /// </summary>
    public class ListingReviewViewModel
    {
        public CarListing Listing { get; set; } = null!;

        public List<SellerInspectionUpload> InspectionUploads { get; set; } = new();

        public InspectionReportFormViewModel InspectionForm { get; set; } = new();

        /// <summary>True once a structured inspection report exists for the car.</summary>
        public bool HasInspectionReport { get; set; }

        public bool IsUsed =>
            string.Equals(Listing.Car?.Type, "Used", System.StringComparison.OrdinalIgnoreCase);

        /// <summary>Used cars need a saved report before approval; New cars never do.</summary>
        public bool CanApprove => !IsUsed || HasInspectionReport;
    }
}
