using gamevault.Helper;
using gamevault.Models;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for CacheImage.xaml
    /// </summary>
    public partial class CacheImage : UserControl
    {
        #region Dependency Property
        public static readonly DependencyProperty ImageCacheTypeProperty = DependencyProperty.Register("ImageCacheType", typeof(ImageCache), typeof(CacheImage));

        public ImageCache ImageCacheType
        {
            get { return (ImageCache)GetValue(ImageCacheTypeProperty); }
            set { SetValue(ImageCacheTypeProperty, value); }
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
        public static readonly DependencyProperty UseUriSourceProperty = DependencyProperty.Register("UseUriSource", typeof(bool), typeof(CacheImage));

        public bool UseUriSource
        {
            get { return (bool)GetValue(UseUriSourceProperty); }
            set { SetValue(UseUriSourceProperty, value); }
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
        #endregion

        public CacheImage()
        {
            InitializeComponent();
        }

        private async Task DataChanged(object newData)
        {
            if (UseUriSource)
            {
                string uri = newData.ToString();
                if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
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
                else
                {
                    if (GifHelper.IsGif(uri))
                    {
                        await GifHelper.LoadGif(uri, uiImg);
                    }
                    else
                    {
                        uiImg.Source = await BitmapHelper.GetBitmapImageAsync(uri);
                    }
                }
                return;
            }

            int identifier = -1;
            int imageId = -1;
            string cachePath = AppFilePath.ImageCache;
            object data = newData;
            if (data == null)
                return;

            try
            {
                if (typeof(Progress) == data.GetType())
                {
                    data = ((Progress)data).Game;
                }
                switch (ImageCacheType)
                {
                    case ImageCache.BoxArt:
                        {
                            cachePath += "/gbox";
                            var game = ((Game)data);
                            identifier = (game == null ? -1 : game.ID);
                            imageId = ((game == null || game.BoxImage == null) ? -1 : game.BoxImage.ID);
                            break;
                        }
                    case ImageCache.GameBackground:
                        {
                            cachePath += "/gbg";
                            var game = ((Game)data);
                            identifier = (game == null ? -1 : game.ID);
                            imageId = ((game == null || game.BackgroundImage == null) ? -1 : game.BackgroundImage.ID);
                            break;
                        }
                    case ImageCache.UserIcon:
                        {
                            cachePath += "/uico";
                            var user = ((User)data);
                            identifier = (user == null ? -1 : user.ID);
                            imageId = ((user == null || user.ProfilePicture == null) ? -1 : user.ProfilePicture.ID);
                            break;
                        }
                    case ImageCache.UserBackground:
                        {
                            cachePath += "/ubg";
                            var user = ((User)data);
                            identifier = (user == null ? -1 : user.ID);
                            imageId = ((user == null || user.BackgroundImage == null) ? -1 : user.BackgroundImage.ID);
                            break;
                        }
                }
            }
            catch (Exception ex) { }
            await CacheHelper.HandleImageCacheAsync(identifier, imageId, cachePath, ImageCacheType, uiImg);
        }
    }
}
