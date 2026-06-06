namespace CarFitProject.ViewModel
{
    /// <summary>
    /// One car make (brand) for the home page "Browse by Make" grid. Built from
    /// approved listings grouped by <c>Car.Make</c>. There is no dedicated Make
    /// entity in the model, so <see cref="LogoUrl"/> is reserved for a future logo
    /// source and is null today (the view falls back to an initial-letter badge).
    /// Make names are brand names (identical across EN/AR), so a single
    /// <see cref="Name"/> serves both cultures.
    /// </summary>
    public class MakeSummaryViewModel
    {
        public string Name { get; set; } = string.Empty;

        public int CarCount { get; set; }

        public string? LogoUrl { get; set; }
    }
}
