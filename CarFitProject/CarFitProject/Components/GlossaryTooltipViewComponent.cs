using CarFitProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Components
{
    public class GlossaryTooltipViewComponent : ViewComponent
    {
        private readonly CarFitDbContext _context;

        public GlossaryTooltipViewComponent(CarFitDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(string rawInspectionText)
        {
            // Pull the full definitions matrix directly from your DB table
            var glossary = await _context.InspectionTermsGlossaries.ToListAsync();

            ViewBag.RawText = rawInspectionText;
            return View(glossary);
        }
    }
}
