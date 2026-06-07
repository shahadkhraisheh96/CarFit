using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    public sealed class CompareViewModel
    {
        public List<CompareCarRow> Cars { get; set; } = new();
    }

    public sealed class CompareCarRow
    {
        public CarListing Listing { get; set; } = null!;
        public decimal? InspectionOverall { get; set; }
        public decimal? InspectionTrust { get; set; }
        public bool IsRisky { get; set; }
        public string EngineStatus { get; set; } = "Unknown";
        public string GearboxStatus { get; set; } = "Unknown";
        public string? PrimaryImageUrl { get; set; }
    }
}
