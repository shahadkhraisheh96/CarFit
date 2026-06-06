using CarFitProject.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CarFitProject.Services
{
    /// <summary>Renders an <see cref="InspectionReport"/> to a print-ready PDF (bilingual, RTL-aware).</summary>
    public interface IInspectionReportPdfService
    {
        byte[] Build(InspectionReport report, IReadOnlyList<InspectionTermsGlossary> glossary, bool isArabic);
    }

    /// <summary>
    /// QuestPDF implementation. Font fallback chain is Cairo → Tahoma → Arial: Cairo (embedded via
    /// wwwroot/fonts) is preferred and required on Linux/prod; Tahoma/Arial are Windows system fonts
    /// that already carry Arabic glyphs so the report renders correctly in dev before Cairo is added.
    /// </summary>
    public class InspectionReportPdfService : IInspectionReportPdfService
    {
        private static readonly string[] FontChain = { "Cairo", "Tahoma", "Arial" };

        private readonly IInspectionScoringService _scoring;

        public InspectionReportPdfService(IInspectionScoringService scoring) => _scoring = scoring;

        public byte[] Build(InspectionReport report, IReadOnlyList<InspectionTermsGlossary> glossary, bool isArabic)
        {
            var s = _scoring.Compute(report);
            var car = report.Car;
            string T(string en, string ar) => isArabic ? ar : en;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(36);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily(FontChain));
                    if (isArabic) page.ContentFromRightToLeft();

                    page.Header().Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("CarFit").FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                            r.ConstantItem(170).AlignRight().Text($"{T("Report", "تقرير")} #{report.CarId}").FontSize(10);
                        });
                        col.Item().Text(T("Vehicle Inspection Report", "تقرير فحص المركبة (ورقة الفحص)")).FontSize(14).SemiBold();
                        col.Item().Text($"{T("Inspection date", "تاريخ الفحص")}: {report.InspectionDate?.ToString("yyyy-MM-dd") ?? "—"}")
                            .FontSize(9).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Spacing(8);

                        col.Item().Text($"{car?.Make} {car?.Model} {car?.Year}").FontSize(13).Bold();
                        col.Item().Text($"{T("Trim", "الفئة")}: {car?.Trim ?? "—"}    {T("Transmission", "ناقل الحركة")}: {car?.Transmission ?? "—"}").FontSize(10);

                        col.Item().PaddingTop(4).Row(r =>
                        {
                            r.Spacing(6);
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(b =>
                            {
                                b.Item().Text(T("Overall score", "النتيجة الإجمالية")).FontColor(Colors.Grey.Darken1).FontSize(9);
                                b.Item().Text(s.OverallScore.ToString("0.00")).FontSize(18).Bold();
                            });
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(b =>
                            {
                                b.Item().Text(T("Trust score (/5)", "درجة الثقة (/٥)")).FontColor(Colors.Grey.Darken1).FontSize(9);
                                b.Item().Text(s.CalculatedTrustScore.ToString("0.00")).FontSize(18).Bold();
                            });
                            r.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(b =>
                            {
                                b.Item().Text("CarSeer").FontColor(Colors.Grey.Darken1).FontSize(9);
                                b.Item().Text(report.CarseerAttached == true ? T("Attached", "مرفق") : "—").FontSize(13).Bold();
                            });
                        });

                        if (s.IsRisky)
                        {
                            col.Item().Background(Colors.Red.Lighten4).Padding(6)
                                .Text(T("⚠ Structural risk flagged on this vehicle", "⚠ خطر هيكلي على هذه المركبة"))
                                .FontColor(Colors.Red.Darken2).Bold();
                        }

                        col.Item().PaddingTop(6).Text(T("Inspection details", "تفاصيل الفحص")).SemiBold();
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void Row(string label, string? val)
                            {
                                t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(label).FontColor(Colors.Grey.Darken1).FontSize(10);
                                t.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4)
                                    .Text(string.IsNullOrWhiteSpace(val) ? "—" : val).FontSize(10);
                            }
                            Row(T("Chassis 1", "الشاصي ١"), report.Chassis1Status);
                            Row(T("Chassis 2", "الشاصي ٢"), report.Chassis2Status);
                            Row(T("Chassis 3", "الشاصي ٣"), report.Chassis3Status);
                            Row(T("Chassis 4", "الشاصي ٤"), report.Chassis4Status);
                            Row(T("Body", "الهيكل"), report.BodyCondition);
                            Row(T("Paint", "الدهان"), report.PaintStatus);
                            Row(T("Roof", "السقف"), report.RoofCondition);
                            Row(T("Engine health %", "صحة المحرك %"), report.EngineHealthPercent?.ToString());
                            Row(T("Gearbox", "ناقل الحركة"), report.GearboxStatus);
                        });

                        col.Item().PaddingTop(8).Text(T("Glossary — Jordanian chassis scale", "مسرد المصطلحات — سلّم الشاصي الأردني")).SemiBold();
                        foreach (var g in glossary)
                        {
                            col.Item().Text(line =>
                            {
                                line.Span($"{g.Term}: ").SemiBold().FontSize(9);
                                line.Span(isArabic ? (g.ExplanationAr ?? "") : (g.ExplanationEn ?? "")).FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                        }

                        col.Item().PaddingTop(14).Row(r =>
                        {
                            r.RelativeItem().Text($"{T("Inspector / center", "الفاحص / المركز")}: {report.CenterName ?? "—"}").FontSize(10);
                            r.RelativeItem().AlignRight().Text(T("Signature: ________________", "التوقيع: ________________")).FontSize(10);
                        });
                    });

                    page.Footer().AlignCenter().Text(line =>
                    {
                        line.Span($"{T("Generated by CarFit", "أُنشئ بواسطة CarFit")} · ").FontSize(8).FontColor(Colors.Grey.Darken1);
                        line.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8).FontColor(Colors.Grey.Darken1);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
