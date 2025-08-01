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
    public class ShowMappedTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            bool showMappedTitle = Preferences.Get(AppConfigKey.ShowMappedTitle,LoginManager.Instance.GetUserProfile().UserConfigFile) == "1";
            if (showMappedTitle && value is Game game && !string.IsNullOrWhiteSpace(game?.Metadata?.Title))
            {
                return true;
            }
            return false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
