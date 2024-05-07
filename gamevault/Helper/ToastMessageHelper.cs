using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    public class ToastMessageHelper
    {
        public static void CreateToastMessage(string title, string message)
        {
            try
            {
                new ToastContentBuilder().AddText(title).AddText(message).Show();
            }
            catch { }
        }
    }
}
