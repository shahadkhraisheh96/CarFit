using CarFitProject.Data;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    public class InspectionController : Controller
    {
        private readonly CarFitDbContext _context;

        public InspectionController(CarFitDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult Book() => View(new InspectionBookingViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(InspectionBookingViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _context.InspectionBookings.Add(new InspectionBooking
                {
                    CustomerName = model.CustomerName,
                    CustomerEmail = model.CustomerEmail,
                    PackageType = model.PackageType,
                    PreferredDate = model.PreferredDate,
                    VehicleNotes = model.VehicleNotes,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Your CarFit evaluation request has been processed successfully!";
                return RedirectToAction(nameof(Book));
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "An unhandled issue occurred while saving your data to SQL Server. Please try again.");
                return View(model);
            }
        }

        // FR-6.4: dedicated entry point for the mechanic-visit form rendered on the
        // listing detail page. Persists an InspectionBooking with CarListingId and
        // (optionally) the chosen MechanicId.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookMechanic(MechanicBookingFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Please double-check your details and try again.";
                return Redirect($"/Inventory/Detail/{model.CarListingId}");
            }

            var listing = await _context.CarListings
                .AsNoTracking()
                .Where(l => l.Id == model.CarListingId)
                .Select(l => new { l.Id })
                .FirstOrDefaultAsync();
            if (listing == null)
            {
                TempData["ErrorMessage"] = "We couldn't find that listing.";
                return Redirect("/Inventory/Search");
            }

            var mechanicId = model.MechanicId > 0 ? model.MechanicId : (int?)null;
            if (mechanicId.HasValue)
            {
                var exists = await _context.Mechanics.AnyAsync(m => m.Id == mechanicId.Value);
                if (!exists) mechanicId = null;
            }

            _context.InspectionBookings.Add(new InspectionBooking
            {
                CustomerName = model.CustomerName.Trim(),
                CustomerEmail = model.CustomerEmail.Trim(),
                PackageType = string.IsNullOrWhiteSpace(model.PackageType) ? "Mechanic visit" : model.PackageType.Trim(),
                PreferredDate = model.PreferredDate,
                VehicleNotes = model.VehicleNotes,
                CarListingId = model.CarListingId,
                MechanicId = mechanicId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Mechanic visit requested — we'll be in touch shortly.";
            return Redirect($"/Inventory/Detail/{model.CarListingId}");
        }
    }
}
