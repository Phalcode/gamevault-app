using gamevault.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace gamevault.Helper
{
    internal class AutostartHelper
    {
        internal static void RegistryCreateAutostartKey()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string exePath = $"{Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)}\\gamevault.exe";
            rk.SetValue("GameVault", exePath);
        }
        internal static void RegistryDeleteAutostartKey()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            if (rk.GetValue("GameVault") != null)
            {
                rk.DeleteValue("GameVault");
            }
        }
        internal static bool RegistryAutoStartKeyExists()
        {
            RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return rk.GetValue("GameVault") != null;
        }
        internal async static Task<bool> IsWindowsPackageAutostartEnabled()
        {
            StartupTask startupTask = await StartupTask.GetAsync("AutostartGameVault");
            return startupTask.State == StartupTaskState.Enabled;
        }
        internal async static Task HandleWindowsPackageAutostart()
        {
            StartupTask startupTask = await StartupTask.GetAsync("AutostartGameVault");
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                    StartupTaskState newState = await startupTask.RequestEnableAsync();
                    if (newState != StartupTaskState.Enabled)
                    {
                        MainWindowViewModel.Instance.AppBarText = "Unable to activate autostart.";
                    }
                    break;
                case StartupTaskState.DisabledByUser:
                    MainWindowViewModel.Instance.AppBarText = "Autostart was disabled manually. Please access task manager to reactivate it.";
                    break;
                case StartupTaskState.DisabledByPolicy:
                    MainWindowViewModel.Instance.AppBarText = "Autostart is either disabled due to policy or not supported on your device.";
                    break;
                case StartupTaskState.Enabled:
                    startupTask.Disable();
                    break;
            }
        }
    }
}
