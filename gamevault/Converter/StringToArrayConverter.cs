using ImageMagick;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class StringToArrayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value == null)
                    return new string[] { };

                if (value.GetType().IsArray)//If its array, then the viewmodel was set insted of the UI 
                {
                    var joined = string.Join(",", (string[])value);
                    return joined;
                }
                else
                {
                    return value.ToString().Split(new[] { parameter.ToString() }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim())
                              .ToArray();
                }
            }
            catch { return new string[] { }; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                return value.ToString().Split(new[] { parameter.ToString() }, StringSplitOptions.None)
                          .Select(s => s.Trim())
                          .ToArray();
            }
            catch { return null; }
        }
    }
}
