using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin view over Phase-6 inspection bookings: list/filter/paginate, detail, and a status
    /// workflow (Confirm / Mark Completed / Cancel) that stamps StatusUpdatedAt as the change log.
    /// Reads the existing InspectionBooking entity — no new tables.
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InspectionBookingsController : Controller
    {
        private const int PageSize = 20;

        // The lifecycle states an admin can move a booking through.
        private static readonly string[] AllowedStatuses = { "Pending", "Confirmed", "Completed", "Cancelled" };

        private readonly CarFitDbContext _context;
        private readonly ILogger<InspectionBookingsController> _logger;

        public InspectionBookingsController(CarFitDbContext context, ILogger<InspectionBookingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? status = null, DateTime? from = null, DateTime? to = null, string? q = null, int page = 1)
        {
            var query = _context.InspectionBookings
                .AsNoTracking()
                .Include(b => b.CarListing).ThenInclude(l => l!.Car)
                .Include(b => b.Mechanic)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(b => b.Status == status);

            if (from.HasValue)
                query = query.Where(b => b.PreferredDate >= from.Value.Date);

            if (to.HasValue)
            {
                var end = to.Value.Date.AddDays(1); // inclusive of the whole 'to' day
                query = query.Where(b => b.PreferredDate < end);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(b => b.CustomerName.Contains(term) || b.CustomerEmail.Contains(term));
            }

            query = query.OrderByDescending(b => b.CreatedAt);

            var vm = new AdminBookingListViewModel
            {
                Rows = await PaginatedList<InspectionBooking>.CreateAsync(query, page, PageSize),
                Status = status,
                From = from,
                To = to,
                Q = q
            };
            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.InspectionBookings
                .AsNoTracking()
                .Include(b => b.CarListing).ThenInclude(l => l!.Car).ThenInclude(c => c!.InspectionReport)
                .Include(b => b.Mechanic)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string? returnTo = null)
        {
            if (!AllowedStatuses.Contains(status))
            {
                TempData["ErrorMessage"] = "Invalid status.";
                return RedirectToAction(nameof(Index));
            }

            var booking = await _context.InspectionBookings.FirstOrDefaultAsync(b => b.Id == id);
            if (booking == null) return NotFound();

            if (booking.Status != status)
            {
                var previous = booking.Status;
                booking.Status = status;
                booking.StatusUpdatedAt = DateTime.UtcNow; // change-log timestamp
                await _context.SaveChangesAsync();
                _logger.LogInformation("Booking {Id} status {Old} -> {New} by admin at {When:o}.",
                    id, previous, status, booking.StatusUpdatedAt);
            }

            TempData["SuccessMessage"] = "Booking status updated.";
            return returnTo == "details"
                ? RedirectToAction(nameof(Details), new { id })
                : RedirectToAction(nameof(Index));
        }
    }
}
