using System.Globalization;

namespace GreenShop
{
    /// <summary>Компактный вывод больших чисел: 1 250 -> 1.25K, 3 400 000 -> 3.4M.</summary>
    public static class NumberFormat
    {
        private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "Qa", "Qi" };

        public static string Compact(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "0";
            }

            bool negative = value < 0.0;
            value = System.Math.Abs(value);

            int tier = 0;
            while (value >= 1000.0 && tier < Suffixes.Length - 1)
            {
                value /= 1000.0;
                tier++;
            }

            string number;
            if (tier == 0)
            {
                number = System.Math.Floor(value).ToString("0", CultureInfo.InvariantCulture);
            }
            else if (value >= 100.0)
            {
                number = value.ToString("0", CultureInfo.InvariantCulture);
            }
            else if (value >= 10.0)
            {
                number = value.ToString("0.#", CultureInfo.InvariantCulture);
            }
            else
            {
                number = value.ToString("0.##", CultureInfo.InvariantCulture);
            }

            return (negative ? "-" : string.Empty) + number + Suffixes[tier];
        }
    }
}
