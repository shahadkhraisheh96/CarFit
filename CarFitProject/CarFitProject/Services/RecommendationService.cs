using CarFitProject.Models;
using CarFitProject.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    public sealed record RecommendationResult(
        List<RecommendedCarViewModel> Cars,
        bool BudgetRelaxed,
        string? RelaxationMessage);

    public interface IRecommendationService
    {
        Task<RecommendationResult> GetMatchesAsync(UserProfile profile, string? userId = null);

        // Backwards-compat shim for any caller that still expects a flat list.
        Task<List<RecommendedCarViewModel>> GetMatchesAsync(UserProfile profile);
    }

    /// <summary>
    /// Real FR-5 ranking. Final = 0.50 × ProfileMatch + 0.30 × InspectionQuality
    /// + 0.20 × BudgetFit. Each component is normalized 0..100 so the final
    /// composite is also 0..100. Top 5 returned.
    ///
    /// ────────── ProfileMatch sub-weights (sum = 100) ──────────
    ///   • Transmission  20 — exact match → 20, car transmission unknown → 10, else 0.
    ///   • Size          20 — explicit body-type match → 20; SUV pref matching SUV/Crossover/Jeep body → 20;
    ///                        seat-count fallback (Small ≤ 5 seats / Medium 4–5 / SUV ≥ 6) → 10; unknown → 5.
    ///   • Family fit    20 — if HasKids && KidsCount > 2: seats ≥ 7 or body contains SUV/MPV/Van → 20, else 0;
    ///                        otherwise (no kid constraint) → 20.
    ///   • Purpose       15 — body-type matches one of the purpose's preferred types → 15;
    ///                        weak match (sedan/hatchback fallback for Work/University) → 7; unknown → 5; else 0.
    ///                        Mapping: Work → Sedan/Hatchback; University → Hatchback/Sedan/Small;
    ///                        Family use → SUV/Sedan/MPV/Van; Travel → SUV/Sedan/Crossover.
    ///   • Condition     15 — Any → 15; New vs Car.Type == "New" → 15 (mismatch 0);
    ///                        Used vs Car.Type == "Used" → 15 (mismatch 0); Car.Type unknown → 7.
    ///   • Trip type     10 — heuristic on engine-size string (Short prefers ≤ 1.6L; Long prefers ≥ 1.8L);
    ///                        when engine_size is missing or unparsable → 5 (neutral).
    ///
    /// ────────── InspectionQuality (0..100) ──────────
    ///   New car OR no inspection report           → 100 (no damage expected).
    ///   IsRisky (chassis cut/replaced / write-off) → 5   (hard penalty, ranked last).
    ///   Otherwise: scoring service OverallScore × 10 (clamped 0..100).
    ///
    /// ────────── BudgetFit (0..100) ──────────
    ///   price within [BudgetMin, BudgetMax]                                       → 100.
    ///   price > BudgetMax but ≤ BudgetMax * 1.10                                  → 50.
    ///   price < BudgetMin                                                         → 50.
    ///   else                                                                      → 0.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private const int TopN = 5;

        private readonly CarFitDbContext _context;
        private readonly IInspectionScoringService _scoring;

        public RecommendationService(CarFitDbContext context, IInspectionScoringService scoring)
        {
            _context = context;
            _scoring = scoring;
        }

        public async Task<RecommendationResult> GetMatchesAsync(UserProfile profile, string? userId = null)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            var budgetMin = profile.BudgetMin ?? 0m;
            var budgetMax = profile.BudgetMax;

            var candidates = await LoadCandidatesAsync(budgetMin, budgetMax);
            bool relaxed = false;
            string? relaxMsg = null;

            if (candidates.Count == 0 && budgetMax > 0)
            {
                var widened = decimal.Round(budgetMax * 1.10m, 2);
                candidates = await LoadCandidatesAsync(budgetMin, widened);
                if (candidates.Count > 0)
                {
                    relaxed = true;
                    relaxMsg = $"Budget widened by 10% (up to {widened:N0} JD) — no exact matches.";
                }
            }

            var scored = candidates
                .Select(c => Score(c, profile))
                .OrderByDescending(r => r.DynamicMatchScore)
                .ThenBy(r => r.IsRisky)
                .ThenBy(r => r.ListingPrice)
                .Take(TopN)
                .ToList();

            if (!string.IsNullOrEmpty(userId) && scored.Count > 0)
            {
                await LogAsync(userId, scored);
            }

            return new RecommendationResult(scored, relaxed, relaxMsg);
        }

        async Task<List<RecommendedCarViewModel>> IRecommendationService.GetMatchesAsync(UserProfile profile)
            => (await GetMatchesAsync(profile, null)).Cars;

        // ------------------------------------------------------------------
        // Data load
        // ------------------------------------------------------------------
        private async Task<List<CandidateRow>> LoadCandidatesAsync(decimal budgetMin, decimal budgetMax)
        {
            var query = _context.CarListings
                .AsNoTracking()
                .Include(l => l.Car)
                    .ThenInclude(c => c!.CarImages)
                .Include(l => l.Car)
                    .ThenInclude(c => c!.InspectionReport)
                .Include(l => l.Seller)
                .Where(l => l.Status == "Active" && l.Car != null);

            if (budgetMax > 0)
            {
                query = query.Where(l =>
                    l.ListingPrice == null ||
                    (l.ListingPrice >= budgetMin && l.ListingPrice <= budgetMax));
            }

            var rows = await query
                .OrderByDescending(l => l.Id)
                .Take(200)
                .ToListAsync();

            return rows
                .Where(l => l.Car != null)
                .Select(l => new CandidateRow(l, l.Car!))
                .ToList();
        }

        // ------------------------------------------------------------------
        // Scoring
        // ------------------------------------------------------------------
        private RecommendedCarViewModel Score(CandidateRow row, UserProfile profile)
        {
            var car = row.Car;
            var listing = row.Listing;
            var price = listing.ListingPrice ?? 0m;
            var reasons = new List<string>();

            var profileMatch = ProfileMatchScore(profile, car, reasons);    // 0..100
            var inspection = InspectionScore(car, reasons);                 // 0..100
            var budget = BudgetScore(price, profile, reasons);              // 0..100

            decimal final = 0.50m * profileMatch + 0.30m * inspection + 0.20m * budget;
            var roundedFinal = (int)Math.Round(final, MidpointRounding.AwayFromZero);

            var primary = car.CarImages?
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .FirstOrDefault();

            var isRisky = car.InspectionReport != null
                && _scoring.Compute(car.InspectionReport).IsRisky;

            return new RecommendedCarViewModel
            {
                CarId = car.Id,
                ListingId = listing.Id,
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                ListingPrice = price,
                City = listing.Seller?.City,
                BodyCondition = car.InspectionReport?.BodyCondition,
                DescriptionScore = car.InspectionReport?.DescriptionScore,
                TrustScore = car.InspectionReport?.CalculatedTrustScore ?? 3.0m,
                DynamicMatchScore = roundedFinal,
                IsRisky = isRisky,
                CarseerAttached = car.InspectionReport?.CarseerAttached ?? false,
                PrimaryImageUrl = primary?.Url,
                MatchReasons = reasons
            };
        }

        private decimal ProfileMatchScore(UserProfile p, Car car, List<string> reasons)
        {
            int transmissionPts = ScoreTransmission(p.TransmissionPref, car.Transmission, reasons);
            int sizePts         = ScoreSize(p.SizePref, car, reasons);
            int familyPts       = ScoreFamilyFit(p.HasKids, p.KidsCount, car, reasons);
            int purposePts      = ScorePurpose(p.Purpose, car.BodyType, reasons);
            int conditionPts    = ScoreCondition(p.ConditionPref, car.Type, reasons);
            int tripPts         = ScoreTrip(p.TripType, car.EngineSize, reasons);

            return transmissionPts + sizePts + familyPts + purposePts + conditionPts + tripPts;
        }

        private int ScoreTransmission(string? pref, string? carTransmission, List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(carTransmission)) return 10;
            if (!string.IsNullOrWhiteSpace(pref) &&
                carTransmission.Equals(pref, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Matches your transmission preference ({pref})");
                return 20;
            }
            return 0;
        }

        private int ScoreSize(string? sizePref, Car car, List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(sizePref)) return 10;
            var body = car.BodyType ?? "";
            if (sizePref.Equals("SUV", StringComparison.OrdinalIgnoreCase))
            {
                if (body.Contains("SUV", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Crossover", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Jeep", StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add($"Matches your size preference (SUV)");
                    return 20;
                }
                return (car.Seats ?? 0) >= 6 ? 10 : 0;
            }
            if (sizePref.Equals("Small", StringComparison.OrdinalIgnoreCase))
            {
                if (body.Contains("Hatchback", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Compact", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Kei", StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add("Matches your size preference (Small)");
                    return 20;
                }
                return (car.Seats ?? 0) is > 0 and <= 5 ? 10 : 0;
            }
            if (sizePref.Equals("Medium", StringComparison.OrdinalIgnoreCase))
            {
                if (body.Contains("Sedan", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Coupe", StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add("Matches your size preference (Medium)");
                    return 20;
                }
                return (car.Seats ?? 0) is >= 4 and <= 5 ? 10 : 0;
            }
            return 5;
        }

        private int ScoreFamilyFit(bool? hasKids, int? kidsCount, Car car, List<string> reasons)
        {
            if (hasKids == true && (kidsCount ?? 0) > 2)
            {
                var seats = car.Seats ?? 0;
                var body = car.BodyType ?? "";
                if (seats >= 7 ||
                    body.Contains("SUV", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("MPV", StringComparison.OrdinalIgnoreCase) ||
                    body.Contains("Van", StringComparison.OrdinalIgnoreCase))
                {
                    reasons.Add(seats >= 7
                        ? $"Fits your family ({seats} seats)"
                        : "Fits your family (SUV/MPV)");
                    return 20;
                }
                return 0;
            }
            return 20;
        }

        private int ScorePurpose(string? purpose, string? bodyType, List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(purpose)) return 7;
            if (string.IsNullOrWhiteSpace(bodyType)) return 5;

            string body = bodyType;
            bool Contains(params string[] keys) => keys.Any(k => body.Contains(k, StringComparison.OrdinalIgnoreCase));

            switch (purpose)
            {
                case "Work":
                    if (Contains("Sedan", "Hatchback")) { reasons.Add("Matches your usage (Work)"); return 15; }
                    return Contains("Coupe") ? 7 : 0;
                case "University":
                    if (Contains("Hatchback", "Sedan", "Compact")) { reasons.Add("Matches your usage (University)"); return 15; }
                    return 7;
                case "Family use":
                    if (Contains("SUV", "Sedan", "MPV", "Van", "Crossover")) { reasons.Add("Matches your usage (Family use)"); return 15; }
                    return 0;
                case "Travel":
                    if (Contains("SUV", "Sedan", "Crossover")) { reasons.Add("Matches your usage (Travel)"); return 15; }
                    return 0;
                default:
                    return 5;
            }
        }

        private int ScoreCondition(string? pref, string? carType, List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(pref) || pref.Equals("Any", StringComparison.OrdinalIgnoreCase))
                return 15;
            if (string.IsNullOrWhiteSpace(carType)) return 7;
            if (pref.Equals(carType, StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add($"Matches your condition preference ({pref})");
                return 15;
            }
            return 0;
        }

        private int ScoreTrip(string? tripType, string? engineSize, List<string> reasons)
        {
            if (string.IsNullOrWhiteSpace(tripType)) return 5;
            if (string.IsNullOrWhiteSpace(engineSize)) return 5;
            if (!TryParseEngineLiters(engineSize, out var liters)) return 5;

            if (tripType.Equals("Short", StringComparison.OrdinalIgnoreCase))
            {
                if (liters <= 1.6m) { reasons.Add("Engine size fits city driving"); return 10; }
                return liters <= 2.0m ? 6 : 3;
            }
            if (tripType.Equals("Long", StringComparison.OrdinalIgnoreCase))
            {
                if (liters >= 1.8m) { reasons.Add("Engine size suits highway trips"); return 10; }
                return liters >= 1.5m ? 6 : 3;
            }
            return 5;
        }

        private decimal InspectionScore(Car car, List<string> reasons)
        {
            var report = car.InspectionReport;
            if (string.Equals(car.Type, "New", StringComparison.OrdinalIgnoreCase))
            {
                reasons.Add("New car — no damage expected");
                return 100m;
            }
            if (report == null)
            {
                reasons.Add("No inspection report on file");
                return 100m;
            }
            var signals = _scoring.Compute(report);
            if (signals.IsRisky)
            {
                reasons.Add("Flagged risky — chassis cut/replaced or write-off");
                return 5m;
            }
            var pct = Math.Min(100m, signals.OverallScore * 10m);
            reasons.Add(pct >= 70m
                ? $"Inspection: جيد {signals.OverallScore:0.0}"
                : pct >= 40m
                    ? $"Inspection: medium ({signals.OverallScore:0.0})"
                    : $"Inspection: poor ({signals.OverallScore:0.0})");
            return pct;
        }

        private decimal BudgetScore(decimal price, UserProfile profile, List<string> reasons)
        {
            var min = profile.BudgetMin ?? 0m;
            var max = profile.BudgetMax;
            if (max <= 0) { reasons.Add("Within your budget"); return 100m; }
            if (price >= min && price <= max)
            {
                reasons.Add("Within your budget");
                return 100m;
            }
            if (price > max && price <= max * 1.10m)
            {
                reasons.Add("Slightly over budget (within 10%)");
                return 50m;
            }
            if (price < min)
            {
                reasons.Add("Under your minimum budget");
                return 50m;
            }
            return 0m;
        }

        private static bool TryParseEngineLiters(string raw, out decimal liters)
        {
            liters = 0m;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var cleaned = new string(raw.Where(c => char.IsDigit(c) || c == '.' || c == ',').ToArray())
                .Replace(',', '.');
            if (string.IsNullOrWhiteSpace(cleaned)) return false;
            if (!decimal.TryParse(cleaned, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var value))
                return false;
            // Heuristic: "1500" or "1500cc" → 1.5L; "1.5" stays as 1.5L.
            if (value > 100) value /= 1000m;
            liters = value;
            return true;
        }

        private async Task LogAsync(string userId, List<RecommendedCarViewModel> top)
        {
            var topScore = top[0].DynamicMatchScore;
            var log = new RecommendationLog
            {
                UserId = userId,
                RecommendedCarIds = string.Join(",", top.Select(t => t.CarId)),
                Score = topScore,
                CreatedAt = DateTime.UtcNow
            };
            _context.RecommendationLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private sealed record CandidateRow(CarListing Listing, Car Car);
    }
}
