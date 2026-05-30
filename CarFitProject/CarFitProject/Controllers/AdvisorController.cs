using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Controllers
{
    // Phase 5 retires the legacy advisor endpoint. The Buyer dashboard is now the
    // single canonical entry point for the FR-5 recommendation flow. Anything that
    // still points at /Advisor/Match is permanently redirected there.
    [Authorize(Roles = "Buyer")]
    public class AdvisorController : Controller
    {
        public IActionResult Match()
            => RedirectToActionPermanent("Index", "Dashboard", new { area = "Buyer" });
    }
}
