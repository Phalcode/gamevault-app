using gamevault.Helper;
using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class ContainsCurrentUserConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value.GetType() == typeof(List<User>))
                {
                    return ((List<User>)value).Any(u => u.ID == LoginManager.Instance.GetCurrentUser().ID);
                }
            }
            catch { }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
