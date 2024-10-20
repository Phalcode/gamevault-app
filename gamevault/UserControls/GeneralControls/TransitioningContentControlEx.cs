using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace gamevault.UserControls
{
    internal class TransitioningContentControlEx : TransitioningContentControl
    {
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            if ((((FrameworkElement)oldContent)?.Tag?.ToString() == "NoPopupTransition") || (((FrameworkElement)newContent)?.Tag?.ToString() == "NoPopupTransition"))
                return;

            base.OnContentChanged(oldContent, newContent);
        }
    }
}
