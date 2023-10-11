using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
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
        public static void SendCrashReport(string errrorMessage, string stackTrace, string unhandledExceptionType)
        {
            try
            {
                string sysInfo = $"Version: {SettingsViewModel.Instance.Version} | Is Windows Package: {(App.IsWindowsPackage == true ? "True" : "False")}";
                try
                {
                    sysInfo += "\n" + GetSysInfo();
                }
                catch { }
                CrashReport crashReport = new CrashReport { source = "GameVault Client", message = $"({unhandledExceptionType}): {errrorMessage}", stackTrace = stackTrace, systemInfo = sysInfo };
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
        public static string GetSysInfo()
        {
            var OS = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
            string os = $"OS: {OS["Caption"]} - {OS["OSArchitecture"]} - Version.{OS["Version"]}"; os = os.Replace("NT 5.1.2600", "XP"); os = os.Replace("NT 5.2.3790", "Server 2003");
            string ram = $"RAM: {OS["TotalVisibleMemorySize"]} KB";
            var CPU = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
            string cpu = $"CPU: {CPU["Name"]} - {CPU["MaxClockSpeed"]} MHz - {CPU["NumberOfCores"]} Core";
            return $"{os}; {ram}; {cpu};";
        }
    }
}
