using CarFitProject.Helpers;
using CarFitProject.Models;

namespace CarFitProject.ViewModel
{
    /// <summary>Admin inspection-bookings index: filtered + paginated, with the filter values echoed back.</summary>
    public class AdminBookingListViewModel
    {
        public PaginatedList<InspectionBooking> Rows { get; set; } = new(new(), 0, 1, 20);
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Q { get; set; }
    }
}
