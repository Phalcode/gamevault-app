using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using MahApps.Metro.Controls;

namespace gamevault.Helper
{
    internal class VisualHelper
    {
        internal static T FindNextParentByType<T>(DependencyObject child)
        {
            DependencyObject parentDepObj = child;
            do
            {
                parentDepObj = VisualTreeHelper.GetParent(parentDepObj);
                if (parentDepObj is T parent) return parent;
            }
            while (parentDepObj != null);
            return default;
        }
        internal static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        internal static void AdjustWindowChrome(MetroWindow window)
        {
            try
            {
                var thumb = (FrameworkElement)window.Template.FindName("PART_WindowTitleThumb", window);
                thumb.Margin = new Thickness(50, 0, 0, 0);
                System.Windows.Controls.Panel.SetZIndex(thumb, 7);
                var btnCommands = (FrameworkElement)window.Template.FindName("PART_WindowButtonCommands", window);
                System.Windows.Controls.Panel.SetZIndex(btnCommands, 8);
            }
            catch { }
        }
        internal static void HideWindow(Window window)
        {
            window.Width = 0;
            window.Height = 0;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Top = int.MinValue;
            window.Left = int.MinValue;
            window.ShowInTaskbar = false;
        }
        internal static void RestoreHiddenWindow(Window window, int height, int width)
        {
            window.Width = width;
            window.Height = height;
            window.ShowInTaskbar = true;
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = window.Width;
            double windowHeight = window.Height;
            window.Left = (screenWidth / 2) - (windowWidth / 2);
            window.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }
}
