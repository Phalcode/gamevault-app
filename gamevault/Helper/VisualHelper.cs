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
    }
}
