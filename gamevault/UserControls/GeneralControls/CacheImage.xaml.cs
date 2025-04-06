using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using gamevault.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel;

namespace gamevault.UserControls
{
    public partial class CacheImage : UserControl
    {
        #region Dependency Property
        public static readonly DependencyProperty ImageCacheTypeProperty = DependencyProperty.Register("ImageCacheType", typeof(ImageCache), typeof(CacheImage));

        public ImageCache ImageCacheType
        {
            get { return (ImageCache)GetValue(ImageCacheTypeProperty); }
            set { SetValue(ImageCacheTypeProperty, value); }
        }
        public static readonly DependencyProperty UseUriSourceProperty = DependencyProperty.Register("UseUriSource", typeof(bool), typeof(CacheImage));

        public bool UseUriSource
        {
            get { return (bool)GetValue(UseUriSourceProperty); }
            set { SetValue(UseUriSourceProperty, value); }
        }
         
        public static readonly DependencyProperty DoNotCacheProperty = DependencyProperty.Register("DoNotCache", typeof(bool), typeof(CacheImage));

        public bool DoNotCache
        {
            get { return (bool)GetValue(DoNotCacheProperty); }
            set { SetValue(DoNotCacheProperty, value); }
        }

        public static readonly DependencyProperty StretchProperty = DependencyProperty.Register("Stretch", typeof(Stretch), typeof(CacheImage), new PropertyMetadata(OnStretchChangedCallBack));

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }
        private static void OnStretchChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if ((Stretch)e.NewValue != Stretch.None)
            {
                ((CacheImage)sender).uiImg.Stretch = (Stretch)e.NewValue;
            }
        }
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(CacheImage), new PropertyMetadata(OnCornerRadiusChangedCallBack));

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        private static void OnCornerRadiusChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((CacheImage)sender).uiBorder.CornerRadius = (CornerRadius)e.NewValue;
        }
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register("Data", typeof(object), typeof(CacheImage), new PropertyMetadata(OnDataChangedCallBack));
        //Add Data as DependencyProperty, because previous DataContext_Changed is called before ImageCacheType is set, when inside XAML DataTemplate. So there was a bug, where i could not choose ImageCacheType. 
        public object Data
        {
            get { return (ImageCache)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }
        private static async void OnDataChangedCallBack(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            await ((CacheImage)sender).DataChanged(e.NewValue);
        }
        #endregion

        #region Convertion Object
        public struct CacheImageMedia
        {
            public int Identifier;
            public int CoverID;
            public int BackgroundID;
            public void Convert(object dataToConvert)
            {
                if (typeof(Game) == dataToConvert.GetType())
                {
                    Identifier = ((Game)dataToConvert) == null ? -1 : ((Game)dataToConvert).ID;
                    CoverID = ((Game)dataToConvert)?.Metadata?.Cover?.ID ?? -1;
                    BackgroundID = ((Game)dataToConvert)?.Metadata?.Background?.ID ?? -1;
                }
                else if (typeof(Progress) == dataToConvert.GetType())
                {
                    Identifier = ((Progress)dataToConvert).Game == null ? -1 : ((Progress)dataToConvert).Game!.ID;
                    CoverID = ((Progress)dataToConvert).Game?.Metadata?.Cover?.ID ?? -1;
                    BackgroundID = ((Progress)dataToConvert).Game?.Metadata?.Background?.ID ?? -1;
                }
                else if (typeof(GameMetadata) == dataToConvert.GetType())
                {
                    Identifier = -1;
                    CoverID = ((GameMetadata)dataToConvert)?.Cover?.ID ?? -1;
                    BackgroundID = ((GameMetadata)dataToConvert)?.Background?.ID ?? -1;
                }
                else if (typeof(User) == dataToConvert.GetType())
                {
                    Identifier = ((User)dataToConvert) == null ? -1 : ((User)dataToConvert).ID;
                    CoverID = ((User)dataToConvert)?.Avatar?.ID ?? -1;
                    BackgroundID = ((User)dataToConvert)?.Background?.ID ?? -1;
                }
            }
        }
        #endregion

        public CacheImage()
        {
            InitializeComponent();
        }

        private async Task DataChanged(object newData)
        {
            if (newData == null)
                return;

            if (UseUriSource)
            {
                string uri = newData.ToString();
                if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                {
                    try
                    {
                        using (MemoryStream stream = new MemoryStream())
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                using (HttpResponseMessage response = await client.GetAsync(uri))
                                {
                                    if (response.IsSuccessStatusCode)
                                    {
                                        await response.Content.CopyToAsync(stream);
                                        stream.Position = 0;
                                        if (GifHelper.IsGif(stream))
                                        {
                                            stream.Position = 0;
                                            await GifHelper.LoadGif(stream, uiImg);
                                        }
                                        else
                                        {
                                            uiImg.Source = await BitmapHelper.GetBitmapImageAsync(stream);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                }
                else
                {
                    try
                    {
                        if (GifHelper.IsGif(uri))
                        {
                            await GifHelper.LoadGif(uri, uiImg);
                        }
                        else
                        {
                            uiImg.Source = BitmapHelper.GetBitmapImage(uri);
                            uiImg.BeginAnimation(System.Windows.Controls.Image.SourceProperty, null);
                        }
                    }
                    catch (Exception ex)
                    {
                        uiImg.Source = CacheHelper.GetReplacementImage(ImageCacheType);
                        //MainWindowViewModel.Instance.AppBarText = ex.Message;
                    }
                }
                return;
            }

            int imageId = -1;
            string cachePath = LoginManager.Instance.GetUserProfile().ImageCacheDir;

            CacheImageMedia media = new CacheImageMedia();
            try
            {
                media.Convert(newData);
                switch (ImageCacheType)
                {
                    case ImageCache.GameCover:
                        {
                            cachePath += "/gbox";
                            imageId = media.CoverID;
                            break;
                        }
                    case ImageCache.GameBackground:
                        {
                            cachePath += "/gbg";
                            imageId = media.BackgroundID;
                            break;
                        }
                    case ImageCache.UserAvatar:
                        {
                            cachePath += "/uico";
                            imageId = media.CoverID;
                            break;
                        }
                    case ImageCache.UserBackground:
                        {
                            cachePath += "/ubg";
                            imageId = media.BackgroundID;
                            break;
                        }
                }
            }
            catch (Exception ex) { }
            if (DoNotCache)
            {
                try
                {
                    if (imageId == -1) { throw new Exception("image id does not exist"); }
                    uiImg.Source = await WebHelper.DownloadImageFromUrlAsync($"{SettingsViewModel.Instance.ServerUrl}/api/media/{imageId}");
                }
                catch (Exception ex)
                {
                    uiImg.Source = CacheHelper.GetReplacementImage(ImageCacheType);
                }
            }
            else
            {
                await CacheHelper.LoadImageCacheToUIAsync(media.Identifier, imageId, cachePath, ImageCacheType, uiImg);
            }
        }
        public ImageSource GetImageSource()
        {
            return uiImg.Source;
        }
    }
}
