using gamevault.Models;
using gamevault.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for MediaSlider.xaml
    /// </summary>
    public partial class MediaSlider : UserControl
    {
        private List<string> MediaUrls = new List<string>();
        private int mediaIndex = 0;
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
            if (uiWebView != null && uiWebView.CoreWebView2 != null)
            {
                uiWebView.Visibility = Visibility.Hidden;
                uiWebView.CoreWebView2?.NavigateToString("<html><body></body></html>");
            }
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
        public async Task LoadFirstElement(string? first = null)
        {
            if (first != null)
            {
                MediaUrls.Add(first);
            }
            if (uiWebView != null && uiWebView.Visibility == Visibility.Visible && MediaUrls.Count > 0)//Prevent only in this case from navigating because the Media Slider could be rendered on top of the game settings
            {
                mediaIndex = 0;
                await MediaSliderNavigate(MediaUrls[mediaIndex]);
                uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
            }
        }
        public bool IsWebViewNull()
        {
            return uiWebView == null;
        }
        public async Task<string> GetCurrentMediaVolume()
        {


            string result = await uiWebView.CoreWebView2.ExecuteScriptAsync(getVolumeScript);
            result = System.Text.RegularExpressions.Regex.Unescape(result.Trim('"'));
            var mediaStatus = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(result);
            double volume = mediaStatus.GetProperty("volume").GetDouble();
            bool isMuted = mediaStatus.GetProperty("muted").GetBoolean();
            if (isMuted)
            {
                return "0.0";
            }
            else
            {
                return volume.ToString().Replace(",", ".");//The media player needs a dot as seperator
            }
        }

        #endregion
        #region Private
        private void ReloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Visible;
            if (MediaUrls.Count > 0)
            {
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex]);
            }
        }
        private async Task ResizeMediaSlider()
        {
            await uiWebView.CoreWebView2.ExecuteScriptAsync(resizescript);
        }
        private async Task MediaSliderNavigate(string url)
        {
            if (uiWebView == null || uiWebView.CoreWebView2 == null)
                return;

            if (uiWebView.Visibility == Visibility.Hidden)
            {
                uiWebView.Visibility = Visibility.Visible;
            }
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
                    MainWindowViewModel.Instance.OpenPopup(this);
                }
                else
                {
                    isMediaSliderFullscreen = false;
                    MainWindowViewModel.Instance.ClosePopup();
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
            if (uiWebView == null)
                return;

            uiWebView.Dispose();
            uiWebView = null;
        }
        #region JS_SCRIPTS
        private string cssscript = @"
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
        private string getVolumeScript = @"
    (function() {
        var video = document.querySelector('video[name=""media""]');
        return JSON.stringify({
            volume: video.volume,
            muted: video.muted
        });
    })();";
        string resizescript = @"
 var video = document.querySelector('video[name=""media""]');
if(video)
{
           "
             + @" video.style.width = 16000 + 'px';
            video.style.height = 9000 + 'px';"
             + @"
            video.style.position = 'fixed'; // Ensure it is positioned to cover the viewport
            video.style.top = '0';
            video.style.left = '0';
            video.style.zIndex = '1000'; // Ensure it is on top of other elements
}
   ";
        #endregion
    }
}
