using crackpipe.Models;
using crackpipe.UserControls;
using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace crackpipe.Converter
{
    internal class PermissionRoleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((User)value != null && ((User)value).Role == PERMISSION_ROLE.ADMIN)
            {
                return true;
            }          
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false;
        }
    }
}
