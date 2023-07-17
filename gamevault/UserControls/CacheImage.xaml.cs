using gamevault.Helper;
using gamevault.Models;
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
        #endregion

        public CacheImage()
        {
            InitializeComponent();
        }

        private async Task DataChanged(object newData)
        {
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
                            identifier = game.ID;
                            imageId = game.BoxImage.ID;
                            break;
                        }
                    case ImageCache.GameBackground:
                        {
                            cachePath += "/gbg";
                            var game = ((Game)data);
                            identifier = game.ID;
                            imageId = game.BackgroundImage.ID;
                            break;
                        }
                    case ImageCache.UserIcon:
                        {
                            cachePath += "/uico";
                            var user = ((User)data);
                            identifier = user.ID;
                            imageId = user.ProfilePicture.ID;
                            break;
                        }
                    case ImageCache.UserBackground:
                        {
                            cachePath += "/ubg";
                            var user = ((User)data);
                            identifier = user.ID;
                            imageId = user.BackgroundImage.ID;
                            break;
                        }
                }
            }
            catch (Exception ex) { }
            uiImg.Source = await CacheHelper.HandleImageCacheAsync(identifier, imageId, cachePath, ImageCacheType);
        }
    }
}
