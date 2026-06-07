using CarFitProject.Helpers;

namespace CarFitProject.ViewModel
{
    /// <summary>One buyer "Buy This Car" lead, enriched with listing + user names for the admin report.</summary>
    public class AdminLeadRow
    {
        public int Id { get; set; }
        public int ListingId { get; set; }
        public string? CarLabel { get; set; }
        public string BuyerName { get; set; } = "";
        public string? SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminLeadsViewModel
    {
        public PaginatedList<AdminLeadRow> Rows { get; set; } = new(new(), 0, 1, 20);
    }
}
