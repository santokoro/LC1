using System.Globalization;

namespace LC1.Lab7
{
    /// <summary>
    /// Вычисление и канонизация литерала на этапе компиляции (свёртка константы).
    /// </summary>
    public static class LiteralNormalizer
    {
        public static bool TryNormalize(string sourceText, string typeName, out string canonical)
        {
            canonical = sourceText;
            if (!TryParseLiteral(sourceText, out double value))
                return false;

            if (typeName == "Double")
                canonical = FormatDouble(value);
            else
                canonical = value.ToString(CultureInfo.InvariantCulture);

            return canonical != sourceText || typeName == "Double";
        }

        public static bool TryParseLiteral(string sourceText, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(sourceText))
                return false;

            return double.TryParse(
                sourceText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }

        public static string FormatDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return value.ToString(CultureInfo.InvariantCulture);

            if (Math.Abs(value % 1) < 1e-12)
                return value.ToString("0.0", CultureInfo.InvariantCulture);

            return value.ToString("G", CultureInfo.InvariantCulture);
        }
    }
}
