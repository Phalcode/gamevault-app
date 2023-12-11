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
            if (value is string stringValue)
            {
                int hashCode = stringValue.GetHashCode();
                Color generatedColor = GenerateColor(hashCode);
                SolidColorBrush brush = new SolidColorBrush(generatedColor);
                brush.Freeze();
                return brush;
            }
            return Brushes.Transparent;
        }

        private Color GenerateColor(int numericValue)
        {
            byte red = (byte)((numericValue >> 16) & 0xFF);
            byte green = (byte)((numericValue >> 8) & 0xFF);
            byte blue = (byte)(numericValue & 0xFF);

            // Darken the color by reducing each component by a fixed amount
            const int darkeningAmount = 10;
            red = (byte)Math.Max(0, red - darkeningAmount);
            green = (byte)Math.Max(0, green - darkeningAmount);
            blue = (byte)Math.Max(0, blue - darkeningAmount);

            return Color.FromRgb(red, green, blue);
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Colors.White;
        }
    }
}
