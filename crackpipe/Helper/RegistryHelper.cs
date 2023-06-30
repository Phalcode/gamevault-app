using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace crackpipe.Helper
{
    internal class RegistryHelper
    {
        internal static void CreateAutostartKey()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string exePath = $"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\crackpipe.exe";
            rk.SetValue("Crackpipe", exePath);
        }
        internal static void DeleteAutostartKey()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk.GetValue("Crackpipe") != null)
            {
                rk.DeleteValue("Crackpipe");
            }
        }
        internal static bool AutoStartKeyExists()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return rk.GetValue("Crackpipe") != null;
        }
    }
}
