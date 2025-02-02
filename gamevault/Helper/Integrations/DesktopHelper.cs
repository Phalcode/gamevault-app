using gamevault.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using gamevault.Models;

namespace gamevault.Helper
{
    public static class DesktopHelper
    {
        public static async Task CreateShortcut(Game game, string iconPath, bool ask)
        {
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = desktopDir + @"\\" + game.Title + ".url";
                if (File.Exists(shortcutPath))
                {
                    MainWindowViewModel.Instance.AppBarText = "Desktop shortcut already exists";
                    return;
                }
                if (ask)
                {
                    MessageDialogResult result = await ((MetroWindow)App.Current.MainWindow).ShowMessageAsync($"Do you want to create a desktop shortcut for {game.Title}?", "",
                    MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Yes", NegativeButtonText = "No", AnimateHide = false });

                    if (result != MessageDialogResult.Affirmative)
                        return;
                }
                using (StreamWriter writer = new StreamWriter(shortcutPath))
                {
                    writer.Write("[InternetShortcut]\r\n");
                    writer.Write($"URL=gamevault://start?gameid={game.ID}" + "\r\n");
                    writer.Write("IconIndex=0\r\n");
                    writer.Write("IconFile=" + iconPath.Replace('\\', '/') + "\r\n");
                    //writer.WriteLine($"WorkingDirectory={Path.GetDirectoryName(SavedExecutable).Replace('\\', '/')}");
                    writer.Flush();
                }

            }
            catch { }
        }
        public static void RemoveShotcut(Game game)
        {
            try
            {
                string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = desktopDir + @"\\" + game.Title + ".url";
                if (File.Exists(shortcutPath))
                {
                    File.Delete(shortcutPath);
                }
            }
            catch { }
        }
        public static bool ShortcutExists(Game game)
        {
            string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string shortcutPath = desktopDir + @"\\" + game.Title + ".url";
            return File.Exists(shortcutPath);
        }
    }
}
