using CarFitProject.Models;
using CarFitProject.Models.Subscriptions;
using CarFitProject.Services.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Controllers
{
    /// <summary>
    /// Stripe webhook sink. Always returns 200 once the signature verifies (even for events we
    /// ignore) so Stripe stops retrying; returns 400 only on a bad/missing signature. Every state
    /// change is idempotent — re-delivered events are safe because payments are keyed on their
    /// Stripe id and subscription rows are upserted by StripeSubscriptionId.
    ///
    /// Note: Stripe's SDK also has a `Subscription` type, so within this file the unqualified
    /// <c>Subscription</c> always means our entity; Stripe types are fully qualified.
    /// </summary>
    [ApiController]
    [Route("Webhooks")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public class WebhooksController : ControllerBase
    {
        private readonly IStripeService _stripe;
        private readonly ISellerSubscriptionService _subs;
        private readonly ISmsService _sms;
        private readonly CarFitDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<WebhooksController> _logger;

        public WebhooksController(
            IStripeService stripe,
            ISellerSubscriptionService subs,
            ISmsService sms,
            CarFitDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<WebhooksController> logger)
        {
            _stripe = stripe;
            _subs = subs;
            _sms = sms;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpPost("Stripe")]
        public async Task<IActionResult> Stripe()
        {
            string json;
            using (var reader = new StreamReader(Request.Body))
            {
                json = await reader.ReadToEndAsync();
            }

            global::Stripe.Event stripeEvent;
            try
            {
                stripeEvent = _stripe.ConstructWebhookEvent(json, Request.Headers["Stripe-Signature"]!);
            }
            catch (global::Stripe.StripeException ex)
            {
                _logger.LogWarning(ex, "Rejected Stripe webhook: bad signature.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe webhook could not be verified.");
                return BadRequest();
            }

            try
            {
                switch (stripeEvent.Type)
                {
                    case global::Stripe.EventTypes.CheckoutSessionCompleted:
                        await HandleCheckoutCompletedAsync((global::Stripe.Checkout.Session)stripeEvent.Data.Object);
                        break;

                    case global::Stripe.EventTypes.CustomerSubscriptionCreated:
                    case global::Stripe.EventTypes.CustomerSubscriptionUpdated:
                        await HandleSubscriptionUpsertAsync((global::Stripe.Subscription)stripeEvent.Data.Object);
                        break;

                    case global::Stripe.EventTypes.CustomerSubscriptionDeleted:
                        await HandleSubscriptionDeletedAsync((global::Stripe.Subscription)stripeEvent.Data.Object);
                        break;

                    case global::Stripe.EventTypes.InvoicePaid:
                        await HandleInvoicePaidAsync((global::Stripe.Invoice)stripeEvent.Data.Object);
                        break;

                    case global::Stripe.EventTypes.InvoicePaymentFailed:
                        await HandleInvoiceFailedAsync((global::Stripe.Invoice)stripeEvent.Data.Object);
                        break;

                    default:
                        _logger.LogDebug("Ignoring unhandled Stripe event {Type}.", stripeEvent.Type);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log but still 200: returning 500 makes Stripe retry, which risks duplicate side
                // effects on a bug. Idempotency keys protect the genuine retry case.
                _logger.LogError(ex, "Error handling Stripe event {Type} ({Id}).", stripeEvent.Type, stripeEvent.Id);
            }

            return Ok();
        }

        // ── checkout.session.completed: only the one-time pay-per-post path needs handling here;
        //    subscription creation is driven by customer.subscription.created. ──
        private async Task HandleCheckoutCompletedAsync(global::Stripe.Checkout.Session session)
        {
            if (session.Mode != "payment") return;
            if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase)) return;

            session.Metadata.TryGetValue("appUserId", out var userId);
            session.Metadata.TryGetValue("planCode", out var planCode);
            session.Metadata.TryGetValue("carListingId", out var carListingRaw);
            if (string.IsNullOrEmpty(userId)) return;

            var plan = planCode is null ? null : await _subs.GetPlanByCodeAsync(planCode);
            int? carListingId = int.TryParse(carListingRaw, out var clid) ? clid : null;

            var (_, created) = await _subs.RecordPaymentIdempotentAsync(new PaymentTransaction
            {
                UserId = userId,
                StripePaymentIntentId = session.PaymentIntentId,
                AmountJod = plan?.AmountJod ?? 0m,
                Status = "Succeeded",
                Type = TransactionType.PerPost,
                CarListingId = carListingId,
                CreatedAt = DateTime.UtcNow
            });

            if (created)
                await NotifyAsync(userId, $"CarFit: your {plan?.AmountJod:0.###} JD listing payment was received.");
        }

        private async Task HandleSubscriptionUpsertAsync(global::Stripe.Subscription sub)
        {
            sub.Metadata.TryGetValue("appUserId", out var userId);
            sub.Metadata.TryGetValue("planCode", out var planCode);
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(planCode)) return;

            var plan = await _subs.GetPlanByCodeAsync(planCode);
            if (plan is null) return;

            var item = sub.Items?.Data?.FirstOrDefault();
            var local = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == sub.Id);

            bool isNew = local is null;
            if (isNew)
            {
                local = new Subscription
                {
                    UserId = userId,
                    PlanId = plan.Id,
                    StripeSubscriptionId = sub.Id,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Subscriptions.Add(local);
            }

            local!.Status = MapStatus(sub.Status);
            local.CurrentPeriodStart = item?.CurrentPeriodStart;
            local.CurrentPeriodEnd = item?.CurrentPeriodEnd;
            if (sub.CancelAtPeriodEnd && local.CancelledAt is null) local.CancelledAt = DateTime.UtcNow;
            if (!sub.CancelAtPeriodEnd) local.CancelledAt = null;

            await _context.SaveChangesAsync();

            if (isNew)
                await NotifyAsync(userId, $"CarFit: your {plan.Name} subscription is active.");
        }

        private async Task HandleSubscriptionDeletedAsync(global::Stripe.Subscription sub)
        {
            var local = await _context.Subscriptions.Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == sub.Id);
            if (local is null) return;

            local.Status = SubscriptionStatus.Cancelled;
            local.CancelledAt ??= DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await NotifyAsync(local.UserId, $"CarFit: your {local.Plan?.Name} subscription has ended.");
        }

        private async Task HandleInvoicePaidAsync(global::Stripe.Invoice invoice)
        {
            var subId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrEmpty(subId)) return;

            var local = await _context.Subscriptions.Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);
            if (local is null) return;

            local.Status = SubscriptionStatus.Active;

            // Idempotent on the invoice id (a stable per-charge Stripe identifier).
            var (_, created) = await _subs.RecordPaymentIdempotentAsync(new PaymentTransaction
            {
                SubscriptionId = local.Id,
                UserId = local.UserId,
                StripePaymentIntentId = invoice.Id,
                AmountJod = local.Plan?.AmountJod ?? 0m,
                Status = "Succeeded",
                Type = TransactionType.Subscription,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            if (created)
                await NotifyAsync(local.UserId, $"CarFit: payment received — your {local.Plan?.Name} subscription has renewed.");
        }

        private async Task HandleInvoiceFailedAsync(global::Stripe.Invoice invoice)
        {
            var subId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
            if (string.IsNullOrEmpty(subId)) return;

            var local = await _context.Subscriptions.Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);
            if (local is null) return;

            local.Status = SubscriptionStatus.PastDue;
            await _context.SaveChangesAsync();

            await NotifyAsync(local.UserId, $"CarFit: we couldn't process your {local.Plan?.Name} payment. Please update your card to keep your subscription active.");
        }

        private async Task NotifyAsync(string userId, string message)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return;
            await _sms.SendAsync(user.PhoneNumber, message);
        }

        private static SubscriptionStatus MapStatus(string stripeStatus) => stripeStatus switch
        {
            "active" or "trialing" => SubscriptionStatus.Active,
            "past_due" or "unpaid" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            "incomplete_expired" => SubscriptionStatus.Expired,
            _ => SubscriptionStatus.Active
        };
    }
}
