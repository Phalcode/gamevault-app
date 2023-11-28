using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace gamevault.Helper
{
    internal class BitmapHelper
    {
        public static BitmapImage GetBitmapImage(string uri)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(uri);
            image.EndInit();
            return image;
        }
        public static MemoryStream BitmapSourceToMemeryStream(BitmapSource src)
        {
            System.Drawing.Bitmap bitmap;
            MemoryStream pasteStream = new MemoryStream();
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(src));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
                bitmap.Save(pasteStream, ImageFormat.Png);
            }
            pasteStream.Seek(0, SeekOrigin.Begin);
            return pasteStream;
        }
    }
}
