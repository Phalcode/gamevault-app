using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace crackpipe.Models
{
    public enum AppConfigKey
    {
        Username,
        Password,
        RootPath,
        Executable,
        BackgroundStart,
        ServerUrl,
        LibStartup
    }
    public static class AppFilePath
    {
        internal static string ImageCache = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/cache/images";
        internal static string OfflineProgress = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/cache/prgs";
        internal static string OfflineCache = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/cache/local";
        internal static string ConfigDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/config";
        internal static string UserFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/config/user";
        internal static string IgnoreList = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/cache/ignorelist";
        internal static string ErrorLog = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/errorlog";
        

        internal static void InitDebugPaths()
        {
            ImageCache = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/cache/images";
            OfflineProgress = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/cache/prgs";
            OfflineCache = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/cache/local";
            ConfigDir = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/config";
            UserFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/config/user";
            IgnoreList = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/cache/ignorelist";
            ErrorLog = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/Crackpipe/debug/errorlog";
        }
    }
}
