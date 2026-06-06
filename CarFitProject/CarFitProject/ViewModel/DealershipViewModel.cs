namespace CarFitProject.ViewModel
{
    public class DealershipViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? City { get; set; }
        public string? Type { get; set; }
        public int ActiveListingsCount { get; set; }
    }
}
