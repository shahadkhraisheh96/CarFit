using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using CarFitProject.Services.Billing;
using CarFitProject.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CarFitProject.Controllers
{
    /// <summary>
    /// Subscription selection, Stripe Checkout entry points, and the seller/buyer billing
    /// dashboard. Accessible to both Dealers (recurring plans) and Buyers (3-day trial then
    /// pay-per-post). The actual subscription/payment state is written by the Stripe webhook,
    /// not here — these actions only kick off Checkout and read current state.
    /// </summary>
    [Authorize(Roles = "Dealer,Buyer")]
    public class BillingController : Controller
    {
        private const int TrialDays = 3;

        private readonly IStripeService _stripe;
        private readonly ISellerSubscriptionService _subs;
        private readonly CarFitDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<BillingController> _logger;

        public BillingController(
            IStripeService stripe,
            ISellerSubscriptionService subs,
            CarFitDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<BillingController> logger)
        {
            _stripe = stripe;
            _subs = subs;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        private string Role => User.IsInRole("Dealer") ? "Dealer" : "Buyer";

        // GET /Billing → role-aware plan picker
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var current = await _subs.GetCurrentAsync(user.Id);
            var vm = new PlanSelectionViewModel
            {
                Role = Role,
                Plans = await _subs.GetActivePlansForRoleAsync(Role),
                Current = current,
                TrialStarted = user.TrialStartedAt.HasValue,
                TrialEndsAt = user.TrialStartedAt?.AddDays(TrialDays),
            };
            vm.PerPostJod = vm.Plans.FirstOrDefault(p => p.BillingInterval == PlanBillingInterval.OneTime)?.AmountJod ?? 0m;
            return View(vm);
        }

        // POST /Billing/Subscribe → start Stripe Checkout for a recurring (dealer) or per-post (buyer) plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(string planCode, int? carListingId = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            if (!_stripe.IsConfigured)
            {
                TempData["ErrorMessage"] = "Payments are temporarily unavailable. Please try again later.";
                return RedirectToAction(nameof(Index));
            }

            var plan = await _subs.GetPlanByCodeAsync(planCode);
            if (plan is null || !plan.IsActive || plan.TargetRole != Role)
            {
                TempData["ErrorMessage"] = "That plan isn't available for your account.";
                return RedirectToAction(nameof(Index));
            }

            var success = Url.Action(nameof(Success), "Billing", null, Request.Scheme)!;
            var cancel = Url.Action(nameof(Cancel), "Billing", null, Request.Scheme)!;

            try
            {
                string url = plan.BillingInterval == PlanBillingInterval.OneTime
                    ? await _stripe.CreatePerPostCheckoutAsync(user, plan, carListingId, success, cancel)
                    : await _stripe.CreateSubscriptionCheckoutAsync(user, plan, success, cancel);
                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe Checkout creation failed for user {UserId}, plan {PlanCode}.", user.Id, planCode);
                TempData["ErrorMessage"] = "Could not start checkout. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST /Billing/StartTrial → Buyer activates the one-time 3-day free trial
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Buyer")]
        public async Task<IActionResult> StartTrial(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            if (user.TrialStartedAt.HasValue)
            {
                TempData["ErrorMessage"] = "You've already used your free trial.";
                return RedirectToAction(nameof(Index));
            }

            var plan = await _subs.GetPlanByCodeAsync("USER_PAYPERPOST");
            if (plan is null)
            {
                TempData["ErrorMessage"] = "Trial is unavailable right now.";
                return RedirectToAction(nameof(Index));
            }

            var now = DateTime.UtcNow;
            user.TrialStartedAt = now;
            await _userManager.UpdateAsync(user);

            _context.Subscriptions.Add(new Subscription
            {
                UserId = user.Id,
                PlanId = plan.Id,
                Status = SubscriptionStatus.Trial,
                TrialEndsAt = now.AddDays(TrialDays),
                CreatedAt = now
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Your {TrialDays}-day free trial has started.";
            return SafeRedirect(returnUrl) ?? RedirectToAction(nameof(Manage));
        }

        // GET /Billing/Success → returned from Stripe Checkout (state is finalized by the webhook)
        public IActionResult Success() => View();

        // GET /Billing/Cancel → user abandoned Checkout
        public IActionResult Cancel() => View();

        // GET /Billing/Manage → current plan, payment history, manage actions
        public async Task<IActionResult> Manage()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var vm = new ManageBillingViewModel
            {
                Current = await _subs.GetCurrentAsync(user.Id),
                History = await _subs.GetPaymentHistoryAsync(user.Id),
                LifetimePaidJod = await _subs.GetLifetimePaidJodAsync(user.Id),
                HasStripeCustomer = !string.IsNullOrEmpty(user.StripeCustomerId),
                PerPostCredits = await _subs.CountUnconsumedPerPostCreditsAsync(user.Id),
                IsDealer = User.IsInRole("Dealer"),
                TrialEndsAt = user.TrialStartedAt?.AddDays(TrialDays)
            };
            return View(vm);
        }

        // POST /Billing/UpdatePaymentMethod → redirect to the Stripe Billing Portal
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentMethod()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            if (string.IsNullOrEmpty(user.StripeCustomerId) || !_stripe.IsConfigured)
            {
                TempData["ErrorMessage"] = "No billing account to manage yet.";
                return RedirectToAction(nameof(Manage));
            }

            var returnUrl = Url.Action(nameof(Manage), "Billing", null, Request.Scheme)!;
            var url = await _stripe.CreateBillingPortalAsync(user.StripeCustomerId, returnUrl);
            return Redirect(url);
        }

        // POST /Billing/CancelSubscription → schedule cancellation at period end (not immediate)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelSubscription()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Challenge();

            var current = await _subs.GetActiveAsync(user.Id);
            if (current is null || string.IsNullOrEmpty(current.StripeSubscriptionId))
            {
                TempData["ErrorMessage"] = "No active subscription to cancel.";
                return RedirectToAction(nameof(Manage));
            }

            try
            {
                await _stripe.CancelAtPeriodEndAsync(current.StripeSubscriptionId);
                current.CancelledAt = DateTime.UtcNow; // stays Active until the period-end webhook
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Your subscription will end at the close of the current billing period.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel subscription {SubId}.", current.StripeSubscriptionId);
                TempData["ErrorMessage"] = "Could not cancel right now. Please try again.";
            }
            return RedirectToAction(nameof(Manage));
        }

        private IActionResult? SafeRedirect(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl) : null;
    }
}
