using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace gamevault.Helper
{
    public static class ScrollViewerHelper
    {
        public static readonly DependencyProperty ScrollBarHeightProperty = DependencyProperty.RegisterAttached("ScrollBarHeight", typeof(int), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(-1, OnScrollBarHeightPropertyChanged));
        public static readonly DependencyProperty EnableHorizontalScrollProperty = DependencyProperty.RegisterAttached("EnableHorizontalScroll", typeof(bool), typeof(ScrollViewerHelper), new FrameworkPropertyMetadata(false, OnEnableHorizontalScrollChanged));

        [AttachedPropertyBrowsableForType(typeof(ScrollViewer))]
        public static int GetScrollBarHeight(DependencyObject obj)
        {
            return (int)obj.GetValue(ScrollBarHeightProperty);
        }
        [AttachedPropertyBrowsableForType(typeof(ScrollViewer))]
        public static void SetScrollBarHeight(DependencyObject obj, int value)
        {
            obj.SetValue(ScrollBarHeightProperty, value);
        }
        [AttachedPropertyBrowsableForType(typeof(ScrollViewer))]
        public static bool GetEnableHorizontalScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnableHorizontalScrollProperty);
        }
        [AttachedPropertyBrowsableForType(typeof(ScrollViewer))]
        public static void SetEnableHorizontalScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(EnableHorizontalScrollProperty, value);
        }
        public static void OnScrollBarHeightPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            int newValue = (int)e.NewValue;
            if (!(sender is ScrollViewer scrollViewer) || newValue < 0)
            {
                return;
            }
            ScrollViewer sc = (ScrollViewer)sender;
            ScrollBar scrollbar = (ScrollBar)sc.Template.FindName("PART_HorizontalScrollBar", sc);
            if (scrollbar != null)
            {
                scrollbar.Height = newValue;
            }
            else
            {
                sc.Loaded += (s, e) =>
                {
                    ScrollBar bar = (ScrollBar)sc.Template.FindName("PART_HorizontalScrollBar", sc);
                    if (bar != null)
                    {
                        bar.Height = newValue;
                    }
                };
            }
        }
        public static void OnEnableHorizontalScrollChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(sender is ScrollViewer scrollViewer) || (bool)e.NewValue == false)
            {
                return;
            }
            ScrollViewer sc = (ScrollViewer)sender;
            sc.PreviewMouseWheel += (s, p) =>
            {
                if (((ScrollViewer)s).ComputedHorizontalScrollBarVisibility == Visibility.Visible)
                {
                    p.Handled = true;
                    if (p.Delta > 0)
                        ((ScrollViewer)s).LineLeft();
                    else
                        ((ScrollViewer)s).LineRight();
                }
                else//Bubble event up to parent if horizontal scrollbar is not needed
                {
                    p.Handled = true;
                    ScrollViewer parent = VisualHelper.FindNextParentByType<ScrollViewer>((ScrollViewer)sender);
                    if (parent != null)
                    {
                        var eventArg = new MouseWheelEventArgs(p.MouseDevice, p.Timestamp, p.Delta);
                        eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                        eventArg.Source = sender;
                        parent.RaiseEvent(eventArg);
                    }
                }
            };
        }
    }
}
