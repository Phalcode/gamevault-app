using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    public class ToastMessageHelper
    {
        public static void CreateToastMessage(string title, string message, string imageUri = "")
        {
            try
            {
                var builder = new ToastContentBuilder().AddText(title).AddText(message);
                if (imageUri != "" && File.Exists(imageUri))
                {
                    builder.AddInlineImage(new Uri(imageUri));
                }
                builder.Show();
            }
            catch { }
        }
    }
}
