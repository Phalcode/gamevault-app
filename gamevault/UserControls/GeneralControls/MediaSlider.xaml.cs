using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for MediaSlider.xaml
    /// </summary>
    public partial class MediaSlider : UserControl
    {
        private List<Tuple<string, string>> MediaUrls = new List<Tuple<string, string>>();
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
                await MediaSliderNavigate(MediaUrls[mediaIndex].Item1);
            }
            else
            {
                mediaIndex = 0;
                await MediaSliderNavigate(MediaUrls[mediaIndex].Item1);
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
                await MediaSliderNavigate(MediaUrls[mediaIndex].Item1);
            }
            else
            {
                mediaIndex = MediaUrls.Count - 1;
                await MediaSliderNavigate(MediaUrls[mediaIndex].Item1);
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
        public async Task RestoreLastMediaVolume()
        {
            string result = Preferences.Get(AppConfigKey.MediaSliderVolume,LoginManager.Instance.GetUserProfile().UserConfigFile);
            string lastMediaVolume = string.IsNullOrWhiteSpace(result) ? "0.0" : result;
            if (double.TryParse(lastMediaVolume.Replace(".", ","), out double volume))
            {
                uiVolumeSlider.Value = volume;
                string restoreLastMediaVolumeScript = @"
    if (typeof audio !== 'undefined' && audio) {
        audio.volume = " + lastMediaVolume + @";
    }
    var video = document.querySelector('video[name=""media""]');
    if (typeof video !== 'undefined' && video) {
        video.volume = " + lastMediaVolume + @";
    }";
                await uiWebView.CoreWebView2.ExecuteScriptAsync(restoreLastMediaVolumeScript);
            }
        }
        public async Task SetAndSaveMediaVolume()
        {
            try
            {
                string result = uiVolumeSlider.Value.ToString().Replace(",", ".");
                Preferences.Set(AppConfigKey.MediaSliderVolume, result,LoginManager.Instance.GetUserProfile().UserConfigFile);
                if (uiWebView == null || uiWebView.CoreWebView2 == null)
                    return;

                string setAndSaveMediaVolumeScript = @"
    if(typeof audio !== 'undefined' && audio) {
        audio.volume=" + result + @";
    }
    if(typeof video !== 'undefined' && video) {
        video.volume=" + result + @";
    }";
                await uiWebView.CoreWebView2.ExecuteScriptAsync(setAndSaveMediaVolumeScript);

            }
            catch { }
        }
        private async void VolumeSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            await SetAndSaveMediaVolume();
        }
        private void DisableNonThumbInteraction(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var slider = sender as Slider;
                if (slider != null)
                {
                    // Get the Thumb control from the Slider
                    var track = slider.Template.FindName("PART_Track", slider) as Track;
                    var thumb = track?.Thumb; // Return the Thumb from the Track
                                              // Check if the mouse is over the Thumb
                    if (thumb != null && !thumb.IsMouseOver)
                    {
                        // Prevent the slider from changing value if clicked outside the Thumb
                        e.Handled = true;
                    }
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
            var env = await CoreWebView2Environment.CreateAsync(null, LoginManager.Instance.GetUserProfile().WebConfigDir, options);
            await uiWebView.EnsureCoreWebView2Async(env);
            uiWebView.NavigationCompleted += async (s, e) =>
            {
                try
                {
                    await CreateAudioStream();
                    await RestoreLastMediaVolume();
                    await ResizeMediaSlider();
                    await uiWebView.CoreWebView2.ExecuteScriptAsync(cssscript);
                }
                catch { }
            };
        }
        public void SetMediaList(List<Tuple<string, string>> mediaUrls)
        {
            this.MediaUrls = mediaUrls;
            uiMediaCountLoadingRing.IsActive = false;
            uiTxtMediaIndex.Visibility = Visibility.Visible;
            uiTxtMediaIndex.Text = $"{mediaIndex + 1}/{MediaUrls.Count}";
        }

        public async Task LoadFirstElement(Tuple<string, string>? first = null)
        {
            if (first != null)
            {
                MediaUrls.Add(first);
            }
            if (uiWebView != null && uiWebView.Visibility == Visibility.Visible && MediaUrls.Count > 0)//Prevent only in this case from navigating because the Media Slider could be rendered on top of the game settings
            {
                mediaIndex = 0;
                await MediaSliderNavigate(MediaUrls[mediaIndex].Item1);
            }
        }
        public bool IsWebViewNull()
        {
            return uiWebView == null;
        }


        #endregion
        #region Private
        private void ReloadMediaSlider()
        {
            uiWebView.Visibility = Visibility.Visible;
            if (MediaUrls.Count > 0)
            {
                uiWebView.CoreWebView2.Navigate(MediaUrls[mediaIndex].Item1);
            }
        }
        private async Task ResizeMediaSlider()
        {
            if (uiWebView == null || uiWebView.CoreWebView2 == null)
                return;

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
            try
            {
                uiWebView.CoreWebView2.Navigate(url);
            }
            catch { }
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
        video::-ms-media-controls-fullscreen-button {
            display: none !important;
        }
      
video::-webkit-media-controls-volume-slider {
display:none;
}

video::-webkit-media-controls-mute-button {
display:none;
}

`;
        style.appendChild(document.createTextNode(cssRules));
        document.head.appendChild(style);
    ";
        private string getVolumeScript = @"
(function() {
    var audio = document.getElementById('externalAudio'); // Get the audio element by ID
    return audio.volume; // Return current volume
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
        private async Task CreateAudioStream()
        {
            string audioScript = @"
    // Select the video element by name attribute 'media'
    var video = document.querySelector('video[name=""media""]');
    if(video)
{
    // Dynamically create an audio element
    var audio = document.createElement('audio');
    audio.setAttribute('id', 'externalAudio');
    audio.src = '" + MediaUrls[mediaIndex].Item2 + @"';  // Specify your external audio source
    audio.controls = false;
    document.body.appendChild(audio);  // Add the audio element to the body

    // Synchronize audio with video playback
    video.addEventListener('play', function() {
        audio.currentTime = video.currentTime;
        audio.play();
    });

    video.addEventListener('pause', function() {
        audio.pause();
    });

    video.addEventListener('seeking', function() {
        audio.currentTime = video.currentTime;
    });

    video.addEventListener('timeupdate', function() {
        var diff = Math.abs(video.currentTime - audio.currentTime);
        if (diff > 0.3) {
            audio.currentTime = video.currentTime;
        }
    });
}
";
            if (MediaUrls[mediaIndex] != null && MediaUrls[mediaIndex].Item2 != "")
            {
                await uiWebView.CoreWebView2.ExecuteScriptAsync(audioScript);
            }
        }




        #endregion

    }
}
