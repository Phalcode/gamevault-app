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
        private static string RootDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault");
        public static string ProfileConfigFile { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "profileconfig");
        public static string ErrorLogDir { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "errorlog");
        public static string PhalcodeDir { get; private set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameVault", "phalcode");
        public static void EnsureRootDirectory()
        {
            if (!Directory.Exists(RootDirectory))
                Directory.CreateDirectory(RootDirectory);
        }
        public static UserProfile CreateUserProfile(string serverUrl)
        {
            string serverRootDirectory = Path.Combine(RootDirectory, serverUrl);
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
            if (!Directory.Exists(RootDirectory))
                Directory.CreateDirectory(RootDirectory);


            List<UserProfile> users = new List<UserProfile>();
            foreach (string serverDir in Directory.GetDirectories(RootDirectory))
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
                                userProfile.ServerUrl = Preferences.Get(AppConfigKey.ServerUrl, userProfile.UserConfigFile);
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
        }
        public static void EnsureUserProfileFileTree(UserProfile userProfile)
        {
            Directory.CreateDirectory(userProfile.ImageCacheDir);
            Directory.CreateDirectory(userProfile.ThemesLoadDir);
            Directory.CreateDirectory(userProfile.WebConfigDir);
            Directory.CreateDirectory(userProfile.CloudSaveConfigDir);           
        }
    }
}
