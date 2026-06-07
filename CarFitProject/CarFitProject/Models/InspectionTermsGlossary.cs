using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class InspectionTermsGlossary
{
    public string Term { get; set; } = null!;

    public string? SeverityLevel { get; set; }

    public string? ExplanationAr { get; set; }

    public string? ExplanationEn { get; set; }
}
