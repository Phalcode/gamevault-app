using crackpipe.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace crackpipe.Converter
{
    internal class GameSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                double size = double.Parse(value.ToString());
                size = size / 1000000;
                if (size > 1000)
                {
                    size = size / 1000;
                    size = Math.Round(size, 2);
                    return $"{size} GB";
                }
                size = Math.Round(size, 2);
                return $"{size} MB";
            }
            catch (Exception ex)
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
