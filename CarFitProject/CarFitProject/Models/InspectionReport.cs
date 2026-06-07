using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class InspectionReport
{
    public int CarId { get; set; }

    public string? CenterName { get; set; }

    public DateOnly? InspectionDate { get; set; }

    public string? Chassis1Status { get; set; }

    public string? Chassis2Status { get; set; }

    public string? Chassis3Status { get; set; }

    public string? Chassis4Status { get; set; }

    public string? BodyCondition { get; set; }

    public string? RoofCondition { get; set; }

    public int? EngineHealthPercent { get; set; }

    public bool? EngineSmoke { get; set; }

    public string? GearboxStatus { get; set; }

    public string? PaintFillerStatus { get; set; }

    public bool? CarseerAttached { get; set; }

    public decimal? OverallScore { get; set; }

    public string? DescriptionScore { get; set; }

    public decimal? CalculatedTrustScore { get; set; }

    public string? PaintStatus { get; set; }

    public virtual Car Car { get; set; } = null!;
}
