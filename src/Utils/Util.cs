using System.Globalization;

namespace Ares.src.Utils
{
    public class Util
    {
        public static bool IsValidUrl(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            {
                return uri != null && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            }

            return false;
        }

        public static string CapitalizeFirstLetter(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return str.Substring(0, 1).ToUpper() + str.Substring(1).ToLower();
        }

        public static string FormatPrice(double value)
        {
            if (value == 0)
                return "0";

            string formatted = value.ToString("G", CultureInfo.InvariantCulture);

            // Garante pelo menos 2 dígitos significativos
            if (value < 1)
            {
                int firstNonZeroIndex = formatted.IndexOfAny("123456789".ToCharArray());
                if (firstNonZeroIndex != -1 && formatted.Length > firstNonZeroIndex + 2)
                    formatted = formatted.Substring(0, firstNonZeroIndex + 3);
            }
            else
            {
                formatted = value.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return formatted;
        }
    }
}