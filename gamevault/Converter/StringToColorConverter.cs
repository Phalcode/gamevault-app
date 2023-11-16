using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace gamevault.Converter
{
    internal class StringToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int hash = value.GetHashCode();

            byte red = (byte)(hash & 0xFF);
            byte green = (byte)((hash >> 8) & 0xFF);
            byte blue = (byte)((hash >> 16) & 0xFF);

            return new SolidColorBrush(Color.FromRgb(red, green, blue));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Colors.White;
        }
    }
}
