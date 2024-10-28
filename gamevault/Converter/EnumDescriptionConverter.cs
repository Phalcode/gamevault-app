using gamevault.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is GameType && (GameType)value != GameType.UNDETECTABLE)
            {
                string result = ((DescriptionAttribute[])typeof(GameType).GetField(((GameType)value).ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false))[0].Description.ToString();
                return result;
            }
            else if (value is State)
            {
                string result = ((DescriptionAttribute[])typeof(State).GetField(((State)value).ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false))[0].Description.ToString();
                return result;
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Empty;
        }
    }
}
