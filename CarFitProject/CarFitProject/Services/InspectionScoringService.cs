using CarFitProject.Models;

namespace CarFitProject.Services
{
    /// <summary>Engine condition derived from the report's health % and smoke flag (FR-4.5).</summary>
    public enum EngineStatus { Unknown, Good, Weak, Unsafe }

    /// <summary>Gearbox condition derived from the report's gearbox status text (FR-4.5).</summary>
    public enum GearboxStatus { Unknown, Good, Knocking }

    /// <summary>
    /// Computed inspection signals returned by <see cref="IInspectionScoringService.Compute"/>.
    /// All values are derived from the raw <see cref="InspectionReport"/> inputs.
    /// </summary>
    public sealed record InspectionDerivedSignals(
        decimal OverallScore,
        decimal CalculatedTrustScore,
        bool IsRisky,
        EngineStatus Engine,
        GearboxStatus Gearbox);

    /// <summary>
    /// Scores a <see cref="InspectionReport"/> per the official Jordanian
    /// inspection scale (FR-4.2, FR-4.3, FR-4.5). Risky cars (شاصي مقصوص /
    /// خالي قص قلبان) are flagged regardless of overall score.
    /// </summary>
    public interface IInspectionScoringService
    {
        /// <summary>Allowed chassis enum values in spec severity order. Used for both UI lists and server validation.</summary>
        IReadOnlyList<string> ChassisTerms { get; }

        /// <summary>Pure function — derives overall score, trust score, Risky flag, and engine/gearbox bands from the report.</summary>
        InspectionDerivedSignals Compute(InspectionReport report);

        /// <summary>Computes signals and writes OverallScore + CalculatedTrustScore back onto the report instance.</summary>
        void ApplyTo(InspectionReport report);
    }

    /// <summary>
    /// Default <see cref="IInspectionScoringService"/> implementation. Stateless
    /// and safe to register as a singleton. Scoring rules are documented per-method.
    /// </summary>
    public class InspectionScoringService : IInspectionScoringService
    {
        // Per-chassis penalty in spec severity order (least to most severe).
        // Sum is 0 when all four chassis are جيد; max is 40 (4 × 10) when all four
        // are write-offs. The overall score is then 0–9.99 (decimal(3,2) cap) and
        // CalculatedTrustScore is half of that, matching the 0–5 trust scale.
        private static readonly Dictionary<string, int> Penalty = new(StringComparer.Ordinal)
        {
            ["جيد"]                = 0,
            ["قصعة شنكل"]          = 0,
            ["دقة على الرأس"]      = 1,
            ["ضربة على الرأس"]     = 2,
            ["ضربة رأسية"]         = 3,
            ["مضروب"]              = 5,
            ["مضروب ومشغول"]       = 6,
            ["شاصي مقصوص ومغير"]   = 8,
            ["خالي قص قلبان"]      = 10
        };

        private static readonly HashSet<string> RiskyTerms = new(StringComparer.Ordinal)
        {
            "شاصي مقصوص ومغير",
            "خالي قص قلبان"
        };

        /// <inheritdoc/>
        public IReadOnlyList<string> ChassisTerms => InspectionGlossarySeed.ChassisTerms;

        /// <summary>
        /// Computes the overall score (0.00–9.99), trust score (0.00–4.99),
        /// Risky flag, and engine/gearbox bands from a report's raw fields.
        /// Throws <see cref="ArgumentNullException"/> if <paramref name="r"/> is null.
        /// </summary>
        public InspectionDerivedSignals Compute(InspectionReport r)
        {
            if (r == null) throw new ArgumentNullException(nameof(r));

            var chassis = new[] { r.Chassis1Status, r.Chassis2Status, r.Chassis3Status, r.Chassis4Status };
            int chassisPenalty = chassis.Sum(c => Penalty.TryGetValue(c ?? "", out var p) ? p : 0);
            int bodyPenalty = SurchargePenalty(r.BodyCondition);
            int paintPenalty = SurchargePenalty(r.PaintStatus);
            int total = chassisPenalty + bodyPenalty + paintPenalty;

            // Start from 9.99 so a clean inspection sits at the decimal(3,2) max and
            // can't overflow the column. Each penalty point peels 0.25 off the score.
            decimal overall = Math.Max(0m, 9.99m - total * 0.25m);
            decimal trust = Math.Max(0m, overall / 2m);

            overall = Math.Round(overall, 2, MidpointRounding.AwayFromZero);
            trust = Math.Round(trust, 2, MidpointRounding.AwayFromZero);

            bool isRisky = chassis.Any(c => c != null && RiskyTerms.Contains(c));

            var engine = (r.EngineSmoke == true) ? EngineStatus.Unsafe
                : !r.EngineHealthPercent.HasValue ? EngineStatus.Unknown
                : r.EngineHealthPercent.Value >= 60 ? EngineStatus.Good
                : r.EngineHealthPercent.Value >= 50 ? EngineStatus.Weak
                : EngineStatus.Unsafe;

            var gearbox = string.IsNullOrWhiteSpace(r.GearboxStatus) ? GearboxStatus.Unknown
                : r.GearboxStatus.Equals("Knocking", StringComparison.OrdinalIgnoreCase) ? GearboxStatus.Knocking
                : GearboxStatus.Good;

            return new InspectionDerivedSignals(overall, trust, isRisky, engine, gearbox);
        }

        /// <summary>
        /// Convenience helper for the inspection persistence path: runs
        /// <see cref="Compute"/> and copies the resulting scores back onto the
        /// supplied <paramref name="report"/> instance prior to SaveChangesAsync.
        /// </summary>
        public void ApplyTo(InspectionReport report)
        {
            var signals = Compute(report);
            report.OverallScore = signals.OverallScore;
            report.CalculatedTrustScore = signals.CalculatedTrustScore;
        }

        // Body / paint columns are free text. We only know "this looks clean" when
        // the field is empty or matches an explicit clean keyword; anything else
        // counts as a single penalty point (caps at 1 each, so 2 total surcharge).
        private static int SurchargePenalty(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            var v = value.Trim();
            if (v == "جيد") return 0;
            if (v.Equals("Good", StringComparison.OrdinalIgnoreCase)) return 0;
            if (v.Equals("Excellent", StringComparison.OrdinalIgnoreCase)) return 0;
            if (v.Equals("OK", StringComparison.OrdinalIgnoreCase)) return 0;
            return 1;
        }
    }
}
