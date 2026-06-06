using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class QuestionnaireStep1ViewModel
    {
        [Required(ErrorMessage = "Please give this profile a name.")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters.")]
        [Display(Name = "Profile name")]
        public string ProfileName { get; set; } = "My Fit Profile";

        [Required(ErrorMessage = "Please enter your age.")]
        [Range(18, 100, ErrorMessage = "Age must be between 18 and 100.")]
        public int Age { get; set; }

        [Required(ErrorMessage = "Please choose a marital status.")]
        [RegularExpression("^(Single|Married)$", ErrorMessage = "Please pick Single or Married.")]
        [Display(Name = "Marital status")]
        public string MaritalStatus { get; set; } = "Single";
    }

    public class QuestionnaireStep2ViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Please tell us whether you have kids.")]
        [Display(Name = "Do you have kids?")]
        public bool HasKids { get; set; }

        [Range(1, 20, ErrorMessage = "Please enter at least 1.")]
        [Display(Name = "Number of kids")]
        public int? KidsCount { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (HasKids && (!KidsCount.HasValue || KidsCount.Value < 1))
            {
                yield return new ValidationResult(
                    "Please enter how many kids you have.",
                    new[] { nameof(KidsCount) });
            }
        }
    }

    public class QuestionnaireStep3ViewModel
    {
        [Required(ErrorMessage = "Please pick a primary purpose.")]
        [RegularExpression("^(Work|University|Family use|Travel)$", ErrorMessage = "Please pick one of the listed options.")]
        [Display(Name = "Primary purpose")]
        public string Purpose { get; set; } = "Work";

        [Required(ErrorMessage = "Please pick a trip type.")]
        [RegularExpression("^(Short|Long)$", ErrorMessage = "Please pick Short or Long.")]
        [Display(Name = "Trip type")]
        public string TripType { get; set; } = "Short";
    }

    public class QuestionnaireStep4ViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Please enter a minimum budget.")]
        [Range(0, 9_999_999, ErrorMessage = "Budget must be a positive number.")]
        [Display(Name = "Minimum budget (JD)")]
        public decimal BudgetMin { get; set; }

        [Required(ErrorMessage = "Please enter a maximum budget.")]
        [Range(0, 9_999_999, ErrorMessage = "Budget must be a positive number.")]
        [Display(Name = "Maximum budget (JD)")]
        public decimal BudgetMax { get; set; }

        [Required(ErrorMessage = "Please pick a payment method.")]
        [RegularExpression("^(Cash|Installments)$", ErrorMessage = "Please pick Cash or Installments.")]
        [Display(Name = "Payment method")]
        public string PaymentMethod { get; set; } = "Cash";

        [Range(1, 120, ErrorMessage = "Please enter a number of months between 1 and 120.")]
        [Display(Name = "Installment months")]
        public int? InstallmentMonths { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BudgetMin >= BudgetMax)
            {
                yield return new ValidationResult(
                    "Minimum budget must be less than maximum budget.",
                    new[] { nameof(BudgetMin), nameof(BudgetMax) });
            }

            if (PaymentMethod == "Installments" && (!InstallmentMonths.HasValue || InstallmentMonths.Value < 1))
            {
                yield return new ValidationResult(
                    "Please enter how many months of installments.",
                    new[] { nameof(InstallmentMonths) });
            }
        }
    }

    public class QuestionnaireStep5ViewModel
    {
        [Required(ErrorMessage = "Please pick a condition preference.")]
        [RegularExpression("^(New|Used|Any)$", ErrorMessage = "Please pick New, Used, or No Preference.")]
        [Display(Name = "Condition")]
        public string ConditionPref { get; set; } = "Any";

        [Required(ErrorMessage = "Please pick a transmission preference.")]
        [RegularExpression("^(Automatic|Manual)$", ErrorMessage = "Please pick Automatic or Manual.")]
        [Display(Name = "Transmission")]
        public string TransmissionPref { get; set; } = "Automatic";

        [Required(ErrorMessage = "Please pick a size preference.")]
        [RegularExpression("^(Small|Medium|SUV)$", ErrorMessage = "Please pick Small, Medium, or SUV.")]
        [Display(Name = "Size")]
        public string SizePref { get; set; } = "Medium";
    }
}
