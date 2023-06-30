using crackpipe.Helper;
using crackpipe.Models;
using crackpipe.ViewModels;
using ImageMagick;
using System.IO;
using System;
using System.Windows.Controls;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows;
using System.Text.Json;
using System.Net;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Linq;

namespace crackpipe.UserControls
{
    /// <summary>
    /// Interaction logic for SettingsUserControl.xaml
    /// </summary>
    public partial class SettingsUserControl : UserControl
    {
        private SettingsViewModel ViewModel { get; set; }
        public SettingsUserControl()
        {
            InitializeComponent();
            ViewModel = SettingsViewModel.Instance;
            this.DataContext = ViewModel;
        }

        private void ClearImageCache_Clicked(object sender, RoutedEventArgs e)
        {
            ViewModel.IsOnIdle = false;
            try
            {
                Directory.Delete(AppFilePath.ImageCache, true);
                Directory.CreateDirectory(AppFilePath.ImageCache);
                MainWindowViewModel.Instance.AppBarText = "Image cache cleared";
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = "Something went wrong while the image cache was cleared";
            }
            ViewModel.IsOnIdle = true;
        }
        private void ClearOfflineCache_Clicked(object sender, RoutedEventArgs e)
        {
            ViewModel.IsOnIdle = false;
            try
            {
                if(File.Exists(AppFilePath.IgnoreList))
                {
                    File.Delete(AppFilePath.IgnoreList);
                }
                if (File.Exists(AppFilePath.OfflineCache))
                {
                    File.Delete(AppFilePath.OfflineCache);
                }               
                MainWindowViewModel.Instance.AppBarText = "Offline cache cleared";
            }
            catch
            {
                MainWindowViewModel.Instance.AppBarText = "Something went wrong while the offline cache was cleared";
            }
            ViewModel.IsOnIdle = true;
        }
    }
}
