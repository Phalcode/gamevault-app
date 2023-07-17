using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class GameTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value != null)
                {
                    int minutes = (int)value;
                    if (minutes < 60)
                    {
                        return $"{minutes} min";
                    }
                    double hours = (double)minutes / 60;
                    hours = Math.Round(hours, 1);
                    return $"{hours} h";
                }
                return "?";
            }
            catch (Exception ex)
            {
                return "?";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
