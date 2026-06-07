using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    public sealed class AdminAnalyticsViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalActiveListings { get; set; }
        public int TotalGlossaryTerms { get; set; }

        public List<TopRecommendedCarRow> TopRecommended { get; set; } = new();
        public List<TopSearchTermRow> TopSearchTerms { get; set; } = new();
        public List<RegistrationsByMonthRow> RegistrationsByMonth { get; set; } = new();
        public List<CarListing> RecentListings { get; set; } = new();
    }

    public sealed record TopRecommendedCarRow(int CarId, string Label, int Count);
    public sealed record TopSearchTermRow(string Term, int Count);
    public sealed record RegistrationsByMonthRow(int Year, int Month, int Count)
    {
        public string Label => new DateTime(Year, Month, 1).ToString("MMM yy");
    }
}
