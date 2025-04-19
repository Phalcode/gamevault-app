using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Models
{
    internal class UserProfile : ViewModelBase
    {

        public UserProfile(string rootDirectory, string imageCacheDir)
        {
            RootDir = rootDirectory;
            ImageCacheDir = imageCacheDir;
            ThemesLoadDir = Path.Combine(rootDirectory, "themes");
            WebConfigDir = Path.Combine(rootDirectory, "config", "web");
            CloudSaveConfigDir = Path.Combine(rootDirectory, "config", "cloudsave");
            CacheDir = Path.Combine(rootDirectory, "cache");

            OfflineProgress = Path.Combine(rootDirectory, "cache", "prgs");
            OfflineCache = Path.Combine(rootDirectory, "cache", "local");
            IgnoreList = Path.Combine(rootDirectory, "cache", "ignorelist");
            UserConfigFile = Path.Combine(rootDirectory, "config", "user");
        }
        public string UserCacheAvatar { get; set; }
        public string RootDir { get; set; }
        private string name { get; set; }
        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged(); }
        }
        public string ServerUrl { get; set; }

        //Directories
        public string ImageCacheDir { get; set; }
        public string ThemesLoadDir { get; set; }
        public string WebConfigDir { get; set; }
        public string CloudSaveConfigDir { get; set; }
        public string CacheDir { get; set; }

        //Files
        public string OfflineProgress { get; set; }
        public string OfflineCache { get; set; }
        public string UserConfigFile { get; set; }
        public string IgnoreList { get; set; }


    }
}
