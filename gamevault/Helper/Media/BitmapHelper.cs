using SkiaSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
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
        public static async Task<BitmapImage> GetBitmapImageAsync(string uri)
        {
            BitmapImage bitmap = null;

            var httpclient = new HttpClient();

            using (var response = await httpclient.GetAsync(uri))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (var stream = new MemoryStream())
                    {
                        await response.Content.CopyToAsync(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }
                }
            }

            return bitmap;
        }
        public static async Task<BitmapImage> GetBitmapImageAsync(MemoryStream stream)
        {
            BitmapImage bitmap = null;
            stream.Position = 0;
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        public static async Task<MemoryStream> UrlToMemoryStream(string url)
        {
            MemoryStream stream = new MemoryStream();
            using (HttpClient client = new HttpClient())
            {
                using (HttpResponseMessage response = await client.GetAsync(url))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        await response.Content.CopyToAsync(stream);
                        stream.Position = 0;
                    }
                }
            }
            return stream;
        }
        public static MemoryStream UriToMemoryStream(string url)
        {
            MemoryStream ms = new MemoryStream();
            using (FileStream file = new FileStream(url, FileMode.Open, FileAccess.Read))
                file.CopyTo(ms);

            ms.Position = 0;
            return ms;
        }
        public static MemoryStream BitmapSourceToMemoryStream(BitmapSource src)
        {
            System.Drawing.Bitmap bitmap;
            MemoryStream pasteStream = new MemoryStream();
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new JpegBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(src));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
                bitmap.Save(pasteStream, ImageFormat.Jpeg);
            }
            pasteStream.Seek(0, SeekOrigin.Begin);
            return pasteStream;
        }
    }
}
