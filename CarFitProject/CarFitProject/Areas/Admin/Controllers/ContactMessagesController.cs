using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ContactMessagesController : Controller
    {
        private const int PageSize = 20;
        private readonly CarFitDbContext _context;

        public ContactMessagesController(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var query = _context.ContactMessages
                .AsNoTracking()
                .OrderByDescending(m => m.CreatedAt);

            var rows = await PaginatedList<ContactMessage>.CreateAsync(query, page, PageSize);
            return View(rows);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null && message.Status != "Read")
            {
                message.Status = "Read";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message marked as read.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Message deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
