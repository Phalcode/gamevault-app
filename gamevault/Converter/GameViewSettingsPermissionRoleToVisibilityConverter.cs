using gamevault.Helper;
using gamevault.Models;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class GameViewSettingsPermissionRoleToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInstalled = (bool)value;
            if (isInstalled || ((LoginManager.Instance.GetCurrentUser() != null && LoginManager.Instance.GetCurrentUser().Role >= PERMISSION_ROLE.EDITOR)))
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
