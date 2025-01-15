using gamevault.Helper;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace gamevault.Windows
{
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow
    {
        private StoreHelper m_StoreHelper;
        public UpdateWindow()
        {
            InitializeComponent();
        }

        private async void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Topmost = true;
                this.Topmost = false;                

                m_StoreHelper = new StoreHelper();
                if (true == await m_StoreHelper.UpdatesAvailable())
                {
                    uiTxtStatus.Text = "Updating...";
                    await m_StoreHelper.DownloadAndInstallAllUpdatesAsync(this);
                }
                App.IsWindowsPackage = true;
            }
            catch (COMException comEx)
            {
                //Is no MSIX package
            }
            catch (Exception ex)
            {
                //rest of the cases
            }
            try
            {
                if (App.IsWindowsPackage == false)
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        httpClient.DefaultRequestHeaders.Add("User-Agent", "Other");
                        var response = await httpClient.GetStringAsync("https://api.github.com/repos/Phalcode/gamevault-app/releases");
                        dynamic obj = JsonNode.Parse(response);
                        string version = (string)obj[0]["tag_name"];
                        if (Convert.ToInt32(version.Replace(".", "")) > Convert.ToInt32(SettingsViewModel.Instance.Version.Replace(".", "")))
                        {
                            MessageBoxResult result = MessageBox.Show($"A new version of GameVault is now available on GitHub.\nCurrent Version '{SettingsViewModel.Instance.Version}' -> new Version '{version}'\nWould you like to download it? (No automatic installation)", "Info", MessageBoxButton.YesNo, MessageBoxImage.Information);
                            if (result == MessageBoxResult.Yes)
                            {
                                string downloadUrl = (string)obj[0]["assets"][0]["browser_download_url"];
                                Process.Start(new ProcessStartInfo(downloadUrl) { UseShellExecute = true });
                                App.Current.Shutdown();
                            }
                        }
                    }
                }
            }
            catch { }
            try
            {
                uiTxtStatus.Text = "Optimizing cache...";
                await CacheHelper.OptimizeCache();
            }
            catch { }
            this.Close();
        }
    }
}
