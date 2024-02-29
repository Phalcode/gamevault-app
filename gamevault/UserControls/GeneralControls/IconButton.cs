using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace gamevault.UserControls
{
    public enum ButtonKind
    {
        Primary,
        Skeleton,
        Danger
    }
    public class IconButton : ButtonBase
    {
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register("CornerRadius", typeof(CornerRadius), typeof(IconButton));
        public static readonly DependencyProperty IconProperty =
           DependencyProperty.Register("Icon", typeof(Geometry), typeof(IconButton));
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string), typeof(IconButton));
        public static readonly DependencyProperty KindProperty =
            DependencyProperty.Register("Kind", typeof(ButtonKind), typeof(IconButton), new PropertyMetadata(ButtonKind.Primary));
        public static readonly DependencyProperty OverrideIconTransformProperty =
            DependencyProperty.Register("OverrideIconTransform", typeof(bool), typeof(IconButton), new FrameworkPropertyMetadata(true));
        public static readonly DependencyProperty IconScaleProperty =
            DependencyProperty.Register("IconScale", typeof(double), typeof(IconButton), new FrameworkPropertyMetadata(1.0));
        public static readonly DependencyProperty IconPositionProperty =
           DependencyProperty.Register("IconPosition", typeof(Point), typeof(IconButton), new FrameworkPropertyMetadata(new Point(0.5, 0.5)));
        public static readonly DependencyProperty IconMarginProperty =
          DependencyProperty.Register("IconMargin", typeof(Thickness), typeof(IconButton), new FrameworkPropertyMetadata(new Thickness(0)));


        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }
        public Geometry Icon
        {
            get { return (Geometry)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }
        public ButtonKind Kind
        {
            get { return (ButtonKind)GetValue(KindProperty); }
            set { SetValue(KindProperty, value); }
        }
        public bool OverrideIconTransform
        {
            get { return (bool)GetValue(OverrideIconTransformProperty); }
            set { SetValue(OverrideIconTransformProperty, value); }
        }
        public double IconScale
        {
            get { return (double)GetValue(IconScaleProperty); }
            set { SetValue(IconScaleProperty, value); }
        }
        public Point IconPosition
        {
            get { return (Point)GetValue(IconPositionProperty); }
            set { SetValue(IconPositionProperty, value); }
        }
        public Thickness IconMargin
        {
            get { return (Thickness)GetValue(IconMarginProperty); }
            set { SetValue(IconMarginProperty, value); }
        }
        public IconButton()
        {
        }
    }
}
