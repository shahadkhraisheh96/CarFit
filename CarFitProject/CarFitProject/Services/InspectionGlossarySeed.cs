using CarFitProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CarFitProject.Services
{
    /// <summary>
    /// Spec glossary (docs/REQUIREMENTS.md §5). Seeded idempotently at startup —
    /// missing terms are inserted, present terms are left alone so admin edits via
    /// GlossaryManager are never overwritten.
    /// </summary>
    public static class InspectionGlossarySeed
    {
        public sealed record GlossaryEntry(string Term, string En, string Ar, string Severity, bool IsChassisTerm);

        public static readonly IReadOnlyList<GlossaryEntry> SpecEntries = new List<GlossaryEntry>
        {
            new("جيد",                "Good",                  "No damage. No penalty on score or price.",                                                "None",     true),
            new("قصعة شنكل",          "Loading dent",          "Very minor dent from shipping. Near-zero effect on safety or value.",                     "Low",      true),
            new("دقة على الرأس",      "Light head tap",        "Minor front/rear hit. Chassis bent ~1cm. Minimal impact on price.",                       "Low",      true),
            new("ضربة على الرأس",     "Head hit",              "Front/rear hit. Chassis bent 3-5cm. Small price impact.",                                 "Medium",   true),
            new("ضربة رأسية",         "Direct head blow",      "Chassis bent 5-10cm. Slight safety concern. Tens of JD price reduction.",                 "Medium",   true),
            new("مضروب",              "Damaged",               "Heavy accident. Chassis badly bent. Needs repair. Hundreds of JD reduction per chassis.", "High",     true),
            new("مضروب ومشغول",       "Damaged & repaired",    "Was badly damaged but repaired. Safety still a concern. Significant price reduction.",    "High",     true),
            new("شاصي مقصوص ومغير",   "Chassis cut & replaced","Chassis was cut and replaced. FLAGGED as Risky. Requires official technical inspection.", "Critical", true),
            new("خالي قص قلبان",      "Complete write-off",    "All 4 chassis points damaged. Car is considered structurally compromised.",               "Critical", true),
            new("دخان أزرق/أبيض",     "Blue/white smoke",      "Engine problem. Indicates oil or coolant burn. Expensive to fix.",                        "High",     false),
            new("طقطقة أكس",          "Knocking axle",         "Gearbox/axle joint issue. Sign of wear. Cost to repair varies by model.",                 "Medium",   false)
        };

        /// <summary>
        /// The constrained chassis-term set used by the chassis dropdowns in the
        /// inspection-report form, in spec severity order (least to most severe).
        /// </summary>
        public static IReadOnlyList<string> ChassisTerms { get; } =
            SpecEntries.Where(e => e.IsChassisTerm).Select(e => e.Term).ToList();

        public static async Task SeedAsync(CarFitDbContext context, CancellationToken ct = default)
        {
            var existing = await context.InspectionTermsGlossaries
                .Select(g => g.Term)
                .ToListAsync(ct);
            var existingSet = new HashSet<string>(existing, StringComparer.Ordinal);

            var missing = SpecEntries
                .Where(e => !existingSet.Contains(e.Term))
                .Select(e => new InspectionTermsGlossary
                {
                    Term = e.Term,
                    ExplanationEn = e.En,
                    ExplanationAr = e.Ar,
                    SeverityLevel = e.Severity
                })
                .ToList();

            if (missing.Count == 0) return;
            await context.InspectionTermsGlossaries.AddRangeAsync(missing, ct);
            await context.SaveChangesAsync(ct);
        }
    }
}
