using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class Mechanic
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? City { get; set; }

    public string? Phone { get; set; }

    public virtual ICollection<InspectionBooking> InspectionBookings { get; set; } = new List<InspectionBooking>();
}
