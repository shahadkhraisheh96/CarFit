using System;
using System.Collections.Generic;

namespace CarFitProject.Models;

public partial class SavedResult
{
    public string UserId { get; set; } = null!;

    public int CarId { get; set; }

    public DateTime? SavedAt { get; set; }

    public virtual Car Car { get; set; } = null!;
}
