using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class GameUpdateAvailableConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                Game game = (Game)value;
                KeyValuePair<Game, string> result = InstallViewModel.Instance.InstalledGames.Where(g => g.Key.ID == game.ID).FirstOrDefault();
                string execFile = Path.Combine(result.Value, "gamevault-exec");
                string installedVersion = Preferences.Get(AppConfigKey.InstalledGameVersion, execFile);
                if(string.IsNullOrWhiteSpace(installedVersion))
                {
                    Preferences.Set(AppConfigKey.InstalledGameVersion, game.Version, execFile);                    
                }
                else if (installedVersion != game.Version)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
