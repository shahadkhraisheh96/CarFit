using System.Text.RegularExpressions;
using CarFitProject.Helpers;
using CarFitProject.Models;
using CarFitProject.Resources;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace CarFitProject.Controllers
{
    /// <summary>
    /// CarFit-owned account helpers that sit alongside the Identity UI. Currently: the
    /// placeholder-phone update prompt that backfilled users are sent to after login.
    /// </summary>
    [Authorize]
    public class AccountController : Controller
    {
        private const string JordanMobile = @"^(?:\+962|00962|0)?7[789]\d{7}$";

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AccountController(UserManager<ApplicationUser> userManager, IStringLocalizer<SharedResource> localizer)
        {
            _userManager = userManager;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> UpdatePhone(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsPlaceholder = user.PhoneIsPlaceholder;
            return View(new UpdatePhoneViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePhone(UpdatePhoneViewModel vm, string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.IsPlaceholder = user.PhoneIsPlaceholder;

            if (string.IsNullOrWhiteSpace(vm.PhoneNumber))
            {
                ModelState.AddModelError(nameof(vm.PhoneNumber), _localizer["Phone number is required"]);
            }
            else if (!Regex.IsMatch(vm.PhoneNumber, JordanMobile))
            {
                ModelState.AddModelError(nameof(vm.PhoneNumber), _localizer["Enter a valid Jordanian mobile number (07XXXXXXXX)"]);
            }

            if (!ModelState.IsValid) return View(vm);

            var normalized = "+" + PhoneHelper.ToWaMeNumber(vm.PhoneNumber);
            if (_userManager.Users.Any(u => u.Id != user.Id && u.PhoneNumber == normalized))
            {
                ModelState.AddModelError(nameof(vm.PhoneNumber), _localizer["This phone number is already registered"]);
                return View(vm);
            }

            user.PhoneNumber = normalized;
            user.PhoneIsPlaceholder = false; // real number now — stop nudging
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = _localizer["Account.UpdatePhone.Saved"].Value;
            return RedirectToLocalOrHome(returnUrl);
        }

        // Lets the user defer; the flag stays set so they'll be prompted again next login.
        [HttpGet]
        public IActionResult SkipPhoneUpdate(string? returnUrl = null) => RedirectToLocalOrHome(returnUrl);

        private IActionResult RedirectToLocalOrHome(string? returnUrl)
            => !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction("Index", "Home");
    }
}
