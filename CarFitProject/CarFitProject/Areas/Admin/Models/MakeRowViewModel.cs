namespace CarFitProject.Areas.Admin.Models
{
    /// <summary>One row in the admin "Manage Makes" table: a <c>Make</c> lookup entry
    /// plus the live count of approved listings carrying that make name.</summary>
    public class MakeRowViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? LogoUrl { get; set; }

        public int CarCount { get; set; }
    }
}
