namespace CarFitProject.ViewModel
{
    /// <summary>
    /// Drives the smart "Add Review" / "Edit Review" button rendered by
    /// <c>ReviewButtonViewComponent</c> on the buyer/seller dashboards and the
    /// home page.
    /// </summary>
    public class ReviewButtonViewModel
    {
        /// <summary>True when the current user already has a review (→ "Edit Review").</summary>
        public bool HasReview { get; set; }

        /// <summary>"button" renders an anchor; "card" renders a dashboard quick-action card.</summary>
        public string Variant { get; set; } = "button";

        /// <summary>CSS classes applied to the anchor in "button" mode.</summary>
        public string CssClass { get; set; } = "btn btn-outline-primary fw-bold";

        /// <summary>Font Awesome icon class.</summary>
        public string IconClass { get; set; } = "fa-solid fa-star";
    }
}
