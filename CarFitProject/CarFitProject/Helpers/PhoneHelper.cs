namespace CarFitProject.Helpers
{
    public static class PhoneHelper
    {
        /// <summary>
        /// Normalizes a Jordanian phone string into the digits-only form expected
        /// by wa.me / WhatsApp deeplinks (E.164 without the '+').
        /// "07-9988-7766" → "962798877666"; "+962 79 988 7766" → "962798877666".
        /// Returns null if the input has no digits.
        /// </summary>
        public static string? ToWaMeNumber(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;

            if (digits.StartsWith("00"))
                digits = digits.Substring(2);
            else if (digits.StartsWith("0"))
                digits = "962" + digits.Substring(1);
            else if (!digits.StartsWith("962"))
                digits = "962" + digits;

            return digits;
        }
    }
}
