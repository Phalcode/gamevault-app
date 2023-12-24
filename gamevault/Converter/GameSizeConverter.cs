using gamevault.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace gamevault.Converter
{
    internal class GameSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                {
                    throw new ArgumentException("Invalid input value");
                }

                double size = double.Parse(value.ToString());

                // DEFAULTS TO IEC (1024) used for storage capacity etc.
                int baseValue = 1024;
                if (parameter != null && parameter is int)
                {
                    // Other Standard could be SI (1000) used for download speeds etc.
                    baseValue = (int)parameter;
                }

                string[] sizeSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

                int suffixIndex = 0;
                while (size >= baseValue && suffixIndex < sizeSuffixes.Length - 1)
                {
                    size /= baseValue;
                    suffixIndex++;
                }

                size = Math.Round(size, 2);
                return $"{size} {sizeSuffixes[suffixIndex]}";
            }
            catch (Exception ex)
            {
                return "ERR";
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
