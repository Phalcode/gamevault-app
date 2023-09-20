using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Helper
{
    public struct CrashReport
    {
        public string source { get; set; }
        public string message { get; set; }
        public string stackTrace { get; set; }
        public string systemInfo { get; set; }
    }

    internal class CrashReportHelper
    {
        public static void SendCrashReport(Exception e, string unhandledExceptionType)
        {
            try
            {
                string sysInfo = $"Version: {SettingsViewModel.Instance.Version} | Is Windows Package: {(App.IsWindowsPackage == true ? "True" : "False")}";
                CrashReport crashReport = new CrashReport { source = "GameVault Client", message = $"({unhandledExceptionType}): {e.Message}", stackTrace = e.StackTrace, systemInfo = sysInfo };
                string parameter = System.Text.Json.JsonSerializer.Serialize(crashReport);
                using (System.Net.WebClient wc = new System.Net.WebClient())
                {
                    wc.Headers[System.Net.HttpRequestHeader.ContentType] = "application/json";
                    string url = Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9waGFsY29kZS1zdXBwb3J0LWJhY2tlbmQucGxhdGZvcm0ucGhhbGNvLmRlL2NyYXNo"));
                    string HtmlResult = wc.UploadString(url, parameter);
                }
            }
            catch (Exception ex)
            {
            }
        }
    }
}
