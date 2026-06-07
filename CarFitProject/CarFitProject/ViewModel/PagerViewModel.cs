namespace CarFitProject.ViewModel
{
    public class PagerViewModel
    {
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string Action { get; set; } = "Index";
        public string? Controller { get; set; }
        public object? RouteValues { get; set; }
    }
}
