using gamevault.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class ProfileManager
    {
        private static string ProfileRootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "profiles");
        public static string ProfileConfigFile { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "profileconfig");
        public static string ErrorLogDir { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "errorlog");
        public static string PhalcodeDir { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "phalcode");
        public static void EnsureRootDirectory()
        {
            if (!Directory.Exists(ProfileRootDirectory))
            {
                Directory.CreateDirectory(ProfileRootDirectory);
                MigrateLegacyCache();
                MoveLegacyCache();
            }
        }
        private static void MigrateLegacyCache()
        {
            string legacyUserCacheFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "config", "user");
            if (File.Exists(legacyUserCacheFile))
            {
                try
                {
                    string username = Preferences.Get("Username", legacyUserCacheFile);
                    string password = Preferences.Get("Password", legacyUserCacheFile, true);
                    string serverurl = Preferences.Get("ServerUrl", legacyUserCacheFile, true);
                    string rootpath = Preferences.Get("RootPath", legacyUserCacheFile);
                    if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(serverurl))
                    {
                        UserProfile profile = CreateUserProfile(WebHelper.RemoveSpecialCharactersFromUrl(serverurl));
                        Preferences.Set(AppConfigKey.ServerUrl, serverurl, profile.UserConfigFile, true);
                        Preferences.Set(AppConfigKey.Username, username, profile.UserConfigFile);
                        Preferences.Set(AppConfigKey.Password, password, profile.UserConfigFile, true);
                        if (Directory.Exists(rootpath))
                        {
                            Preferences.Set(AppConfigKey.RootDirectories, rootpath, profile.UserConfigFile);
                        }
                    }
                }
                catch { }
            }
        }
        private static void MoveLegacyCache()
        {
            try
            {
                string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "cache");
                string config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "config");
                string themes = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "themes");
                string legacyDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "legacy", "1.16.1.0");

                Directory.CreateDirectory(legacyDir);

                if (Directory.Exists(cache))
                {
                    string cacheDestination = Path.Combine(legacyDir, "cache");
                    Directory.Move(cache, cacheDestination);
                }

                if (Directory.Exists(config))
                {
                    string configDestination = Path.Combine(legacyDir, "config");
                    Directory.Move(config, configDestination);
                }

                if (Directory.Exists(themes))
                {
                    string themesDestination = Path.Combine(legacyDir, "themes");
                    Directory.Move(themes, themesDestination);
                }
            }
            catch { }
        }

        public static UserProfile CreateUserProfile(string serverUrl)
        {
            string serverRootDirectory = Path.Combine(ProfileRootDirectory, serverUrl);
            string serverImageCacheDirectory = Path.Combine(serverRootDirectory, "ImageCache");
            Directory.CreateDirectory(serverImageCacheDirectory);
            string userProfileRootDirectory = Path.Combine(serverRootDirectory, Guid.NewGuid().ToString());

            UserProfile userProfile = new UserProfile(userProfileRootDirectory, serverImageCacheDirectory);
            userProfile.UserCacheAvatar = "";//So it will load the default image
            EnsureUserProfileFileTree(userProfile);
            return userProfile;
        }

        public static List<UserProfile> GetUserProfiles()
        {
            if (!Directory.Exists(ProfileRootDirectory))
                Directory.CreateDirectory(ProfileRootDirectory);


            List<UserProfile> users = new List<UserProfile>();
            foreach (string serverDir in Directory.GetDirectories(ProfileRootDirectory))
            {
                foreach (string userDir in Directory.GetDirectories(serverDir))
                {
                    if (Path.GetFileName(userDir).Equals("ImageCache"))
                        continue;

                    foreach (string file in Directory.EnumerateFiles(userDir, "*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if (Path.GetFileName(file).Equals("user"))
                            {
                                UserProfile userProfile = new UserProfile(userDir, Path.Combine(serverDir, "ImageCache"));
                                userProfile.Name = Preferences.Get(AppConfigKey.Username, userProfile.UserConfigFile);
                                userProfile.ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, userProfile.UserConfigFile, true);
                                userProfile.UserCacheAvatar = CacheHelper.GetUserProfileAvatarPath(userProfile);
                                if (string.IsNullOrWhiteSpace(userProfile.ServerUrl) || string.IsNullOrWhiteSpace(userProfile.Name))
                                {
                                    break;
                                }
                                users.Add(userProfile);
                                break;
                            }
                        }
                        catch { }
                    }
                }
            }
            return users;
        }
        public static void DeleteUserProfile(UserProfile userProfile)
        {
            if (Directory.Exists(userProfile.RootDir))
            {
                Directory.Delete(userProfile.RootDir, true);
            }
            string serverRootDir = Path.GetDirectoryName(userProfile.RootDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            var subDirs = Directory.GetDirectories(serverRootDir);
            if (subDirs.Length == 1 && Path.GetFileName(subDirs[0]).Equals("ImageCache", StringComparison.OrdinalIgnoreCase))
            {
                // If the only subdirectory left is the ImageCache, delete the server root directory as well
                Directory.Delete(serverRootDir, true);
            }
        }
        public static void EnsureUserProfileFileTree(UserProfile userProfile)
        {
            try
            {
                if (!Directory.Exists(userProfile.ImageCacheDir))
                    Directory.CreateDirectory(userProfile.ImageCacheDir);

                if (!Directory.Exists(userProfile.ThemesLoadDir))
                    Directory.CreateDirectory(userProfile.ThemesLoadDir);

                if (!Directory.Exists(userProfile.WebConfigDir))
                    Directory.CreateDirectory(userProfile.WebConfigDir);

                if (!Directory.Exists(userProfile.CloudSaveConfigDir))
                    Directory.CreateDirectory(userProfile.CloudSaveConfigDir);

                if (!Directory.Exists(userProfile.CacheDir))
                    Directory.CreateDirectory(userProfile.CacheDir);

            }
            catch { }
        }
    }
}
