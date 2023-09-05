using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class DownloadLimitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double size = double.Parse(value.ToString());
                if (size == 0)
                {
                    return "Unlimited";
                }
                size = size / 1000000;
                if (size > 100)
                {
                    size = size / 1000;
                    size = Math.Round(size, 2);
                    return $"{size} GB/s";
                }
                else if (size > 0.1)
                {
                    size = Math.Round(size, 2);
                    return $"{size} MB/s";
                }
                else if (size > 0.0001)
                {
                    size *= 1000; size = Math.Round(size, 2);
                    return $"{size} KB/s";
                }
                else
                {
                    size *= 1000000; size = Math.Round(size, 2);
                    return $"{size} B/s";
                }
            }
            catch
            {
                return "Calc error";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
