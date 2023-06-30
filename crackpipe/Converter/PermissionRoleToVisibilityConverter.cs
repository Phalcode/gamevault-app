using crackpipe.Helper;
using crackpipe.Models;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace crackpipe.Converter
{
    internal class PermissionRoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((int)value == 0 || (LoginManager.Instance.GetCurrentUser() != null && LoginManager.Instance.GetCurrentUser().Role == PERMISSION_ROLE.ADMIN))
            {
                return Visibility.Visible;
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
