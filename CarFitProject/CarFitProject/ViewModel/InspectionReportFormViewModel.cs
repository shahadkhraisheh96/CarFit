using System.ComponentModel.DataAnnotations;

namespace CarFitProject.ViewModel
{
    public class InspectionReportFormViewModel
    {
        public int CarId { get; set; }

        // Read-only label for the header — "Toyota Camry 2018".
        public string CarLabel { get; set; } = string.Empty;

        // ----- Inspection metadata -----
        [StringLength(100)]
        [Display(Name = "Inspection center")]
        public string? CenterName { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Inspection date")]
        public DateOnly? InspectionDate { get; set; }

        // ----- Chassis (FR-4.2) -----
        [Required(ErrorMessage = "Please pick a status for chassis point 1.")]
        [Display(Name = "Chassis 1")]
        public string Chassis1Status { get; set; } = "جيد";

        [Required(ErrorMessage = "Please pick a status for chassis point 2.")]
        [Display(Name = "Chassis 2")]
        public string Chassis2Status { get; set; } = "جيد";

        [Required(ErrorMessage = "Please pick a status for chassis point 3.")]
        [Display(Name = "Chassis 3")]
        public string Chassis3Status { get; set; } = "جيد";

        [Required(ErrorMessage = "Please pick a status for chassis point 4.")]
        [Display(Name = "Chassis 4")]
        public string Chassis4Status { get; set; } = "جيد";

        // ----- Body & paint -----
        [StringLength(255)]
        [Display(Name = "Body condition")]
        public string? BodyCondition { get; set; }

        [StringLength(100)]
        [Display(Name = "Roof condition")]
        public string? RoofCondition { get; set; }

        [StringLength(255)]
        [Display(Name = "Paint status")]
        public string? PaintStatus { get; set; }

        [Display(Name = "Paint filler notes")]
        public string? PaintFillerStatus { get; set; }

        [StringLength(255)]
        [Display(Name = "Inspection notes")]
        public string? DescriptionScore { get; set; }

        // ----- Engine & gearbox (FR-4.5) -----
        [Range(0, 100, ErrorMessage = "Engine health must be a percentage between 0 and 100.")]
        [Display(Name = "Engine health %")]
        public int? EngineHealthPercent { get; set; }

        [Display(Name = "Smoke present")]
        public bool EngineSmoke { get; set; }

        [RegularExpression("^(Good|Knocking)$", ErrorMessage = "Gearbox must be Good or Knocking.")]
        [Display(Name = "Gearbox")]
        public string? GearboxStatus { get; set; }

        // ----- CarSeer (FR-4.6) -----
        [Display(Name = "CarSeer report attached")]
        public bool CarseerAttached { get; set; }

        // Derived (shown after save / on review).
        public decimal? OverallScore { get; set; }
        public decimal? CalculatedTrustScore { get; set; }
    }
}
