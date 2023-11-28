using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class GameSettingsTabVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (((Game)value) == null)
                return Visibility.Collapsed;
            if (parameter.ToString() == "local")
            {
                if (NewInstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == ((Game)value).ID).Count() > 0)
                {
                    return Visibility.Visible;
                }
            }
            else if (parameter.ToString() == "server")
            {
                if ((LoginManager.Instance.GetCurrentUser() != null && LoginManager.Instance.GetCurrentUser().Role >= PERMISSION_ROLE.EDITOR))
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
