using gamevault.Models;
using gamevault.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Media.Core;
using YoutubeExplode;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for MediaSlider.xaml
    /// </summary>
    public partial class MediaSlider : UserControl
    {
        private List<string> MediaUrls = new List<string>();
        private int mediaIndex = -1;
        private bool isMediaSliderFullscreen = false;
        private Grid webViewAnchor;
        public MediaSlider()
        {
            InitializeComponent();
        }
        #region Events
        private void ReloadMediaSlider_Click(object sender, RoutedEventArgs e)
        {
            ReloadMediaSlider();
        }
        private void MediaSliderFullscreen_Click(object sender, RoutedEventArgs e)
        {
            ToggleFullscreen();
        }
        private void MediaSliderFullscreen_Escape_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (isMediaSliderFullscreen && e.Key == Key.Escape)
            {
                ToggleFullscreen();
            }
        }
        private async void NextMedia_Click(object sender, RoutedEventArgs e)
        {
            if (MediaUrls.Count < 1)
                return;

            if (mediaIndex < MediaUrls.Count - 1)
            {
                mediaIndex++;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = 0;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        private async void PrevMedia_Click(object sender, RoutedEventArgs e)
        {
            if (MediaUrls.Count < 1)
                return;

            if (mediaIndex > 0)
            {
                mediaIndex--;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            else
            {
                mediaIndex = MediaUrls.Count - 1;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
            }
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        #endregion
        #region Public 
        public void UnloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Hidden;
            uiWebView.CoreWebView2?.NavigateToString("<html><body></body></html>");
        }
        public string GetLastMediaVolume()
        {
            string result = Preferences.Get(AppConfigKey.MediaSliderVolume, AppFilePath.UserFile);
            return string.IsNullOrWhiteSpace(result) ? "0.0" : result;
        }
        public async Task SaveMediaVolume()
        {
            try
            {
                if (uiWebView == null || uiWebView.CoreWebView2 == null)
                    return;

                string result = await GetCurrentMediaVolume();
                if (!string.IsNullOrWhiteSpace(result) && result != "null")
                {
                    Preferences.Set(AppConfigKey.MediaSliderVolume, result, AppFilePath.UserFile);
                }
            }
            catch { }
        }
        public async Task InitVideoPlayer()
        {
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = "--disk-cache-size=1000000"
            };
            var env = await CoreWebView2Environment.CreateAsync(null, AppFilePath.WebConfigDir, options);
            await uiWebView.EnsureCoreWebView2Async(env);
            uiWebView.NavigationCompleted += async (s, e) =>
            {
                try
                {
                    string mutescript = @"var video = document.querySelector('video[name=""media""]');if(video){video.volume=" +
                    GetLastMediaVolume() +
                    ";}";

                    string cssscript = @"
        var style = document.createElement('style');
        style.type = 'text/css';
        var cssRules = `video::-webkit-media-controls-fullscreen-button {
            display: none !important;
        }
        video::-moz-media-controls-fullscreen-button {
            display: none !important;
        }
        video::-ms-media-controls-fullscreen-button {
            display: none !important;
        }`;
        style.appendChild(document.createTextNode(cssRules));
        document.head.appendChild(style);
    ";
                    await uiWebView.CoreWebView2.ExecuteScriptAsync(mutescript);
                    await ResizeMediaSlider();
                    await uiWebView.CoreWebView2.ExecuteScriptAsync(cssscript);
                }
                catch { }
            };
        }
        public void SetMediaList(List<string> mediaUrls)
        {
            this.MediaUrls = mediaUrls;
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }
        public void LoadFirstElement()
        {
            NextMedia_Click(null, null);
        }
        public bool IsWebViewNull()
        {
            return uiWebView == null;
        }
        public async Task<string> GetCurrentMediaVolume()
        {
            string resizescript = @"document.querySelector('video[name=""media""]').volume;";
            string result = await uiWebView.CoreWebView2.ExecuteScriptAsync(resizescript);
            return result;
        }
        #endregion
        #region Private
        private void ReloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Visible;
            uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
        }
        private async Task ResizeMediaSlider()
        {
            string resizescript = @"
 var video = document.querySelector('video[name=""media""]');
if(video)
{
           ";


            resizescript += @" video.style.width = 16000 + 'px';
            video.style.height = 9000 + 'px';";


            resizescript += @"
            video.style.position = 'fixed'; // Ensure it is positioned to cover the viewport
            video.style.top = '0';
            video.style.left = '0';
            video.style.zIndex = '1000'; // Ensure it is on top of other elements
}
   ";
            await uiWebView.CoreWebView2.ExecuteScriptAsync(resizescript);
        }
        private async Task MediaSliderNavigate(string url)
        {
            if (uiWebView == null || uiWebView.CoreWebView2 == null)
                return;

            await SaveMediaVolume();
            uiWebView.CoreWebView2.Navigate(url);

        }
        private void ToggleFullscreen()
        {
            try
            {
                if (!isMediaSliderFullscreen)
                {
                    isMediaSliderFullscreen = true;
                    webViewAnchor = (Grid)this.Parent;
                    webViewAnchor.Children.Remove(this);
                    this.Margin = new Thickness(15);
                    MainWindowViewModel.Instance.OpenPopup(this);
                }
                else
                {
                    isMediaSliderFullscreen = false;
                    MainWindowViewModel.Instance.ClosePopup();
                    this.Margin = new Thickness(0);
                    webViewAnchor.Children.Add(this);
                }
            }
            catch (Exception ex)
            {
            }//Probably is the Visual not disconnected from its Parent
        }
        #endregion

        public void Dispose()
        {
            uiWebView.Dispose();
            uiWebView = null;
        }
    }
}
