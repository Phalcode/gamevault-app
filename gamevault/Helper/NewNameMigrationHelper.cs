using gamevault.Models;
using gamevault.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class NewNameMigrationHelper
    {
        private static string OldAppDataDir;
        private static string OldConfigFilePath;
        private static void ReplaceRegOldEntryIfExists()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk.GetValue("Crackpipe") != null)
            {
                rk.DeleteValue("Crackpipe");
                RegistryHelper.CreateAutostartKey();
            }
        }
        private static void RenameAppDataDir()
        {
            if (Directory.Exists(OldAppDataDir))
            {
                Directory.Move(OldAppDataDir, $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/GameVault");
            }
        }
        private static void ReplaceOldRootPathName()
        {
            OldConfigFilePath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/config/user";
            if (File.Exists(OldConfigFilePath))
            {
                string path = Preferences.Get(AppConfigKey.RootPath, OldConfigFilePath);
                if (Directory.Exists(path + "/Crackpipe"))
                {
                    Directory.Move(path + "/Crackpipe", path + "/GameVault");
                }
            }
        }
        public static void MigrateIfNeeded()
        {
            OldAppDataDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe";
            if (Directory.Exists(OldAppDataDir))
            {
                ReplaceOldRootPathName();
                RenameAppDataDir();
                ReplaceRegOldEntryIfExists();
            }
        }
    }
}
