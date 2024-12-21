using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace gamevault.Helper
{
    internal class GifHelper
    {
        private static void LoadNormalGIF(MagickImageCollection collection, System.Windows.Controls.Image img)
        {
            List<double> animationDelays = GetAnimationDelays(collection);
            var animation = new ObjectAnimationUsingKeyFrames();          
            animation.Duration = TimeSpan.FromSeconds(animationDelays.Sum());
            animation.RepeatBehavior = RepeatBehavior.Forever;
            int frameCount = 0;
            foreach (MagickImage image in collection)
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(image.ToByteArray());//MagickFormat.Png
                bitmapImage.EndInit();
                double currentKeyTime = animationDelays.Take(frameCount).Sum();
                var keyFrame = new DiscreteObjectKeyFrame(bitmapImage, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(currentKeyTime)));
                frameCount++;
                animation.KeyFrames.Add(keyFrame);
            }
            animation.Freeze();
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                img.BeginAnimation(System.Windows.Controls.Image.SourceProperty, animation);
            });
        }
        private static void LoadDisposalGIF(MagickImageCollection images, System.Windows.Controls.Image img)
        {
            var animation = new ObjectAnimationUsingKeyFrames();

            List<double> animationDelays = GetAnimationDelays(images);
            animation.Duration = TimeSpan.FromSeconds(animationDelays.Sum());
            animation.RepeatBehavior = RepeatBehavior.Forever;
            int frameCount = 0;
            MagickImage frameImage = null;
            foreach (MagickImage image in images)
            {
                int x = image.Page.X;
                int y = image.Page.Y;
                if (frameCount == 0)
                {
                    frameImage = image;
                }
                else
                {
                    frameImage.Composite(image, x, y, CompositeOperator.Over);
                }
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(frameImage.ToByteArray(MagickFormat.Png));//MagickFormat.Png
                bitmapImage.EndInit();

                double currentKeyTime = animationDelays.Take(frameCount).Sum();
                var keyFrame = new DiscreteObjectKeyFrame(bitmapImage, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(currentKeyTime)));
                frameCount++;
                animation.KeyFrames.Add(keyFrame);
            }
            animation.Freeze();
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                img.BeginAnimation(System.Windows.Controls.Image.SourceProperty, animation);
            });
        }
        private static List<double> GetAnimationDelays(MagickImageCollection frames)
        {
            List<double> animationDelays = new List<double>();
            foreach (var frame in frames)
            {
                var frameDelay = frame.AnimationDelay * 0.01;
                if (frameDelay == 0)
                    frameDelay = 0.1;

                animationDelays.Add(frameDelay);
            }
            return animationDelays;
        }
        private static bool IsDisposableGif(MagickImageCollection frames)
        {
            bool hasDisposalMethod = false;
            hasDisposalMethod = frames.All(image => image.GifDisposeMethod == GifDisposeMethod.None || image.GifDisposeMethod == GifDisposeMethod.Undefined);
            if (hasDisposalMethod)
            {
                return true;
            }
            foreach (MagickImage frame in frames)
            {
                if (frame.GifDisposeMethod == GifDisposeMethod.Previous)
                {
                    return true;
                }
            }
            return hasDisposalMethod;
        }
        internal static bool IsGif(string filePath)
        {
            // GIF signature: 47 49 46 38 39 (47 49 46 37 61 for GIF87a)
            byte[] gifSignature = Encoding.UTF8.GetBytes("GIF");
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[3];
                    fs.Read(buffer, 0, 3);
                    return buffer[0] == gifSignature[0] && buffer[1] == gifSignature[1] && (buffer[2] == gifSignature[2] || buffer[2] == '8' || buffer[2] == '7');
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
        internal static bool IsGif(MemoryStream ms)
        {
            byte[] gifSignature = Encoding.UTF8.GetBytes("GIF");
            try
            {
                byte[] buffer = new byte[3];
                ms.Read(buffer, 0, 3);
                return buffer[0] == gifSignature[0] && buffer[1] == gifSignature[1] && (buffer[2] == gifSignature[2] || buffer[2] == '8' || buffer[2] == '7');
            }
            catch (Exception)
            {
                return false;
            }
        }
        internal static async Task LoadGif(MemoryStream ms, System.Windows.Controls.Image img)
        {
            await Task.Run(() =>
            {
                using (MagickImageCollection frames = new MagickImageCollection(ms))
                {
                    if (IsDisposableGif(frames))
                    {
                        LoadDisposalGIF(frames, img);
                    }
                    else
                    {
                        LoadNormalGIF(frames, img);
                    }
                }
            });
        }
        internal static async Task LoadGif(string path, System.Windows.Controls.Image img)
        {
            await Task.Run(() =>
            {
                using (MagickImageCollection frames = new MagickImageCollection(path))//use apng:{mypath} to load frames for APNG
                {
                    if (IsDisposableGif(frames))
                    {
                        LoadDisposalGIF(frames, img);
                    }
                    else
                    {
                        LoadNormalGIF(frames, img);
                    }
                }
            });
        }
        internal static void OptimizeGIF(string path, uint maxHeightWidth)
        {
            Tuple<int, int>? dimensions = GetGifDimensions(path);
            if (dimensions != null && (dimensions.Item1 > maxHeightWidth || dimensions.Item2 > maxHeightWidth))
            {
                using (MagickImageCollection collection = new MagickImageCollection(path))
                {
                    MagickGeometry size = new MagickGeometry(maxHeightWidth);
                    size.IgnoreAspectRatio = false;
                    // Coalesce the image
                    collection.Coalesce();
                    foreach (MagickImage image in collection)
                    {
                        image.Resize(size);
                        image.GifDisposeMethod = GifDisposeMethod.Background;
                    }
                    //collection.Optimize();
                    collection.Write(path);
                }
            }
        }
        private static Tuple<int, int>? GetGifDimensions(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    // Skip to the start of the logical screen descriptor block
                    fs.Seek(6, SeekOrigin.Begin);

                    byte[] buffer = new byte[4];
                    fs.Read(buffer, 0, 4);

                    // Extract width and height from the buffer
                    int width = BitConverter.ToUInt16(buffer, 0);
                    int height = BitConverter.ToUInt16(buffer, 2);
                    return Tuple.Create(width, height);
                }
            }
            catch (Exception)
            {
                // Handle any exceptions that might occur during file operations
                return null;
            }
        }
    }
}
