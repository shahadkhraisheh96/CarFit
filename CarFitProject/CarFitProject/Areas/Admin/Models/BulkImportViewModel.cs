namespace CarFitProject.Areas.Admin.Models
{
    public class BulkImportViewModel
    {
        public bool IsSuccess { get; set; }
        public int InsertedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> ErrorMessages { get; set; } = new List<string>();
    }

    public class CarListingCsvRow
    {
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
        public string? Trim { get; set; }
        public decimal ListingPrice { get; set; }
    }
}
