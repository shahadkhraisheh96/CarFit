using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Controllers
{
    /// <summary>
    /// Lightweight UI culture switcher (NFR-U1). Persists the selection in the
    /// ASP.NET Core localization cookie so subsequent requests render in the
    /// chosen language, then bounces the user back to where they came from.
    /// </summary>
    public class LanguageController : Controller
    {
        private static readonly string[] Supported = { "en", "ar" };

        [HttpGet]
        public IActionResult Set(string culture, string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(culture) || Array.IndexOf(Supported, culture) < 0)
            {
                culture = "en";
            }

            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    SameSite = SameSiteMode.Lax
                });

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return Redirect("/");
        }
    }
}
