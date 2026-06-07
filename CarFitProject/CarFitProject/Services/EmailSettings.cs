namespace CarFitProject.Services
{
    /// <summary>SMTP configuration bound from the <c>EmailSettings</c> configuration section.</summary>
    public class EmailSettings
    {
        public string? Host { get; set; }

        public int Port { get; set; } = 587;

        public string? User { get; set; }

        public string? Password { get; set; }

        public string? From { get; set; }

        public bool EnableSsl { get; set; } = true;
    }
}
