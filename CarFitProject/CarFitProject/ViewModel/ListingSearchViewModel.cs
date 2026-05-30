using CarFitProject.Helpers;
using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    public class ListingSearchViewModel
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int? YearFrom { get; set; }
        public int? YearTo { get; set; }
        public decimal? PriceFrom { get; set; }
        public decimal? PriceTo { get; set; }
        public string? Type { get; set; }
        public string? Transmission { get; set; }
        public int Page { get; set; } = 1;

        public PaginatedList<CarListing> Results { get; set; } = new(new List<CarListing>(), 0, 1, 12);
    }
}
