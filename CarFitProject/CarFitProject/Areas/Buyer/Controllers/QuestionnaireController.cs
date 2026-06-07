using CarFitProject.Services;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CarFitProject.Areas.Buyer.Controllers
{
    [Area("Buyer")]
    [Authorize(Roles = "Buyer")]
    public class QuestionnaireController : Controller
    {
        private readonly IUserProfileService _profiles;

        public QuestionnaireController(IUserProfileService profiles)
        {
            _profiles = profiles;
        }

        private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        [HttpGet]
        public async Task<IActionResult> Start(int? profileId)
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            if (profileId.HasValue)
            {
                await _profiles.StartFromProfileAsync(HttpContext, UserId, profileId.Value);
            }
            else
            {
                _profiles.ClearDraft(HttpContext);
                _profiles.SaveDraft(HttpContext, new ProfileDraftViewModel { ProfileName = "My Fit Profile" });
            }
            return RedirectToAction(nameof(Step1));
        }

        // ------------------------------------------------------------------
        // Step 1 — Basics
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Step1()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(new QuestionnaireStep1ViewModel
            {
                ProfileName = draft.ProfileName ?? "My Fit Profile",
                Age = draft.Age ?? 18,
                MaritalStatus = draft.MaritalStatus ?? "Single"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step1(QuestionnaireStep1ViewModel vm, string action)
        {
            var draft = _profiles.GetDraft(HttpContext);
            draft.ProfileName = vm.ProfileName;
            draft.Age = vm.Age;
            draft.MaritalStatus = vm.MaritalStatus;
            _profiles.SaveDraft(HttpContext, draft);

            if (action == "back")
            {
                return RedirectToAction("Index", "Profiles");
            }

            if (!ModelState.IsValid) return View(vm);
            return RedirectToAction(nameof(Step2));
        }

        // ------------------------------------------------------------------
        // Step 2 — Family
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Step2()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(new QuestionnaireStep2ViewModel
            {
                HasKids = draft.HasKids ?? false,
                KidsCount = draft.KidsCount
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step2(QuestionnaireStep2ViewModel vm, string action)
        {
            var draft = _profiles.GetDraft(HttpContext);
            draft.HasKids = vm.HasKids;
            draft.KidsCount = vm.HasKids ? vm.KidsCount : null;
            _profiles.SaveDraft(HttpContext, draft);

            if (action == "back") return RedirectToAction(nameof(Step1));
            if (!ModelState.IsValid) return View(vm);
            return RedirectToAction(nameof(Step3));
        }

        // ------------------------------------------------------------------
        // Step 3 — Purpose & Trip
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Step3()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(new QuestionnaireStep3ViewModel
            {
                Purpose = draft.Purpose ?? "Work",
                TripType = draft.TripType ?? "Short"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step3(QuestionnaireStep3ViewModel vm, string action)
        {
            var draft = _profiles.GetDraft(HttpContext);
            draft.Purpose = vm.Purpose;
            draft.TripType = vm.TripType;
            _profiles.SaveDraft(HttpContext, draft);

            if (action == "back") return RedirectToAction(nameof(Step2));
            if (!ModelState.IsValid) return View(vm);
            return RedirectToAction(nameof(Step4));
        }

        // ------------------------------------------------------------------
        // Step 4 — Budget & Payment
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Step4()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(new QuestionnaireStep4ViewModel
            {
                BudgetMin = draft.BudgetMin ?? 0m,
                BudgetMax = draft.BudgetMax ?? 0m,
                PaymentMethod = draft.PaymentMethod ?? "Cash",
                InstallmentMonths = draft.InstallmentMonths
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step4(QuestionnaireStep4ViewModel vm, string action)
        {
            var draft = _profiles.GetDraft(HttpContext);
            draft.BudgetMin = vm.BudgetMin;
            draft.BudgetMax = vm.BudgetMax;
            draft.PaymentMethod = vm.PaymentMethod;
            draft.InstallmentMonths = vm.PaymentMethod == "Installments" ? vm.InstallmentMonths : null;
            _profiles.SaveDraft(HttpContext, draft);

            if (action == "back") return RedirectToAction(nameof(Step3));
            if (!ModelState.IsValid) return View(vm);
            return RedirectToAction(nameof(Step5));
        }

        // ------------------------------------------------------------------
        // Step 5 — Preferences
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Step5()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(new QuestionnaireStep5ViewModel
            {
                ConditionPref = draft.ConditionPref ?? "Any",
                TransmissionPref = draft.TransmissionPref ?? "Automatic",
                SizePref = draft.SizePref ?? "Medium"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Step5(QuestionnaireStep5ViewModel vm, string action)
        {
            var draft = _profiles.GetDraft(HttpContext);
            draft.ConditionPref = vm.ConditionPref;
            draft.TransmissionPref = vm.TransmissionPref;
            draft.SizePref = vm.SizePref;
            _profiles.SaveDraft(HttpContext, draft);

            if (action == "back") return RedirectToAction(nameof(Step4));
            if (!ModelState.IsValid) return View(vm);
            return RedirectToAction(nameof(Review));
        }

        // ------------------------------------------------------------------
        // Step 6 — Review & Submit
        // ------------------------------------------------------------------
        [HttpGet]
        public IActionResult Review()
        {
            var draft = _profiles.GetDraft(HttpContext);
            return View(draft);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit()
        {
            if (string.IsNullOrEmpty(UserId)) return Challenge();

            var draft = _profiles.GetDraft(HttpContext);
            var saved = await _profiles.CompleteAsync(UserId, draft);
            if (saved == null)
            {
                TempData["ErrorMessage"] = "We couldn't find the profile you were editing.";
                return RedirectToAction("Index", "Profiles");
            }

            _profiles.ClearDraft(HttpContext);
            TempData["SuccessMessage"] = "Profile saved.";
            return RedirectToAction("Index", "Dashboard", new { activeProfileId = saved.ProfileId });
        }
    }
}
