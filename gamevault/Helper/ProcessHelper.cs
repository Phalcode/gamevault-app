using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    internal class ProcessHelper
    {
        internal static Process StartApp(string fileName, string parameter = "", bool asAdmin = false)
        {
            ProcessStartInfo app = new ProcessStartInfo();
            if (parameter != "")
            {
                app.Arguments = parameter;
            }
            app.FileName = fileName;
            app.WorkingDirectory = Path.GetDirectoryName(fileName);
            app.UseShellExecute = true;           
            if (asAdmin)
            {
                app.Verb = "runas";
            }
            return Process.Start(app);
        }
    }
}
