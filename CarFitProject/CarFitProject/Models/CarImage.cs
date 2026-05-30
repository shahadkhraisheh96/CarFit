using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarFitProject.Models;

// Maps this class to your physical lowercase database table name
[Table("car_images")]
public partial class CarImage
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("car_id")]
    public int CarId { get; set; }

    [Column("url")]
    public string Url { get; set; } = null!;

    [Column("sort_order")]
    public int SortOrder { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    public virtual Car? Car { get; set; }
}