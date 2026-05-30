namespace CarFitProject.ViewModel
{
    /// <summary>
    /// Session-backed accumulator for the multi-step questionnaire. Each step
    /// reads/writes the slice it owns; the Review step commits the whole thing
    /// to a UserProfile row via IUserProfileService.CompleteAsync.
    /// </summary>
    public class ProfileDraftViewModel
    {
        /// <summary>Non-null when editing an existing profile.</summary>
        public int? ExistingProfileId { get; set; }

        // Step 1 — Basics
        public string? ProfileName { get; set; }
        public int? Age { get; set; }
        public string? MaritalStatus { get; set; }

        // Step 2 — Family
        public bool? HasKids { get; set; }
        public int? KidsCount { get; set; }

        // Step 3 — Purpose & Trip
        public string? Purpose { get; set; }
        public string? TripType { get; set; }

        // Step 4 — Budget & Payment
        public decimal? BudgetMin { get; set; }
        public decimal? BudgetMax { get; set; }
        public string? PaymentMethod { get; set; }
        public int? InstallmentMonths { get; set; }

        // Step 5 — Preferences
        public string? ConditionPref { get; set; }
        public string? TransmissionPref { get; set; }
        public string? SizePref { get; set; }
    }
}
