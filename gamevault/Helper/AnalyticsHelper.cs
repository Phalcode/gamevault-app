using AngleSharp.Io;
using gamevault.UserControls;
using gamevault.ViewModels;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using Windows.UI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace gamevault.Helper
{
    internal class AnalyticsHelper
    {
        #region Singleton
        private static AnalyticsHelper instance = null;
        private static readonly object padlock = new object();

        public static AnalyticsHelper Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new AnalyticsHelper();
                    }
                    return instance;
                }
            }
        }
        #endregion
        private Timer _heartBeatTimer;
        private string timeZone;
        private string language;
        private HttpClient client;
        private bool trackingEnabled = false;
        internal AnalyticsHelper()
        {
            trackingEnabled = SettingsViewModel.Instance.SendAnonymousAnalytics;
#if DEBUG
            trackingEnabled = false;
#endif
            if (!trackingEnabled)
                return;

            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.Clear();
            string plattform = IsWineRunning() ? "Linux" : "Windows NT";
            var userAgent = $"GameVault/{SettingsViewModel.Instance.Version} ({plattform})";
            client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
            try
            {
                TimeZoneInfo.TryConvertWindowsIdToIanaId(TimeZoneInfo.Local.Id, RegionInfo.CurrentRegion.TwoLetterISORegionName, out timeZone);
                language = CultureInfo.CurrentCulture.Name;
            }
            catch { }

        }
        internal void InitHeartBeat()
        {
            if (!trackingEnabled)
                return;

            _heartBeatTimer = new Timer(HeartBeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }
        private void HeartBeat(object state)
        {
            SendHeartBeat(AnalyticsTargets.HB);
        }
        internal void RegisterGlobalEvents()
        {
            if (!trackingEnabled)
                return;

            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.Button), System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(GlobalButton_Click));

            EventManager.RegisterClassHandler(typeof(IconButton), IconButton.ClickEvent, new RoutedEventHandler(GlobalButton_Click));
        }
        private async void GlobalButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is ButtonBase button)
                {
                    string methodName = ParseMethodName(button);
                    var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData() { Event = "BUTTON_CLICK", Metadata = new { Path = methodName }, Timezone = timeZone, Language = language }), Encoding.UTF8, "application/json");
                    await client.PostAsync(AnalyticsTargets.CU, jsonContent);
                }
            }
            catch { }
        }
        private string ParseMethodName(ButtonBase buttonBase)
        {
            var type = buttonBase.GetType();
            var eventHandlersStore = typeof(UIElement).GetProperty("EventHandlersStore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eventHandlersStoreValue = eventHandlersStore.GetValue(buttonBase, null);

            if (eventHandlersStoreValue != null)
            {
                var entriesField = eventHandlersStoreValue.GetType().GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance);
                var entriesValue = entriesField.GetValue(eventHandlersStoreValue);

                var mapStoreField = entriesValue.GetType().GetField("_mapStore", BindingFlags.NonPublic | BindingFlags.Instance);
                var mapStoreValue = mapStoreField.GetValue(entriesValue);

                var entry0Field = mapStoreValue.GetType().GetField("_entry0", BindingFlags.NonPublic | BindingFlags.Instance);
                var entry0Value = entry0Field.GetValue(mapStoreValue);

                var Value = entry0Value.GetType().GetField("Value").GetValue(entry0Value);

                var listStoreField = Value.GetType().GetField("_listStore", BindingFlags.NonPublic | BindingFlags.Instance);
                if (listStoreField == null)
                {
                    string methodName = ((EventSetter)buttonBase.Style.Setters[0]).Handler.Method.Name;
                    string className = ((EventSetter)buttonBase.Style.Setters[0]).Handler.Method.DeclaringType?.Name ?? "UnknownClass";
                    return $"{className}.{methodName}";
                }
                var listStoreValue = listStoreField.GetValue(Value);
                var loneEntryField = listStoreValue.GetType().GetField("_loneEntry", BindingFlags.NonPublic | BindingFlags.Instance);
                var loneEntryValue = loneEntryField.GetValue(listStoreValue);

                if (loneEntryValue.GetType() == typeof(RoutedEventHandlerInfo))
                {
                    string methodName = ((RoutedEventHandlerInfo)loneEntryValue).Handler.Method.Name;
                    string className = ((RoutedEventHandlerInfo)loneEntryValue).Handler?.Method?.DeclaringType?.Name ?? "UnknownClass";
                    if (methodName.EndsWith("b__3"))
                    {
                        return "ConfirmationPopupNo_Click";
                    }
                    else if (methodName.EndsWith("b__4"))
                    {
                        return "ConfirmationPopupYes_Click";
                    }
                    return $"{className}.{methodName}";
                }
            }
            else if (buttonBase.Command != null)
            {
                return ((System.Windows.Input.RoutedCommand)buttonBase.Command).Name;
            }
            return string.Empty;
        }
        private async Task SendHeartBeat(string url)
        {
            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData()), Encoding.UTF8, "application/json");
                await client.PostAsync(url, jsonContent);
                //string responseBody = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e) { }

        }
        public async Task SendPageView(UserControl page)
        {
            if (!trackingEnabled)
                return;

            try
            {
                string? pageString = ParseUserControl(page);
                var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData() { Timezone = timeZone, CurrentPage = pageString, Language = language }), Encoding.UTF8, "application/json");
                await client.PostAsync(AnalyticsTargets.LG, jsonContent);
            }
            catch (Exception e) { }

        }
        public void SendErrorLog(Exception ex)
        {
            if (!trackingEnabled)
                return;

            Task.Run(async () =>
            {
                try
                {
                    var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData() { ExceptionType = ex.GetType().ToString(), ExceptionMessage = $"Message:{ex.Message} | InnerException:{ex.InnerException?.Message} | StackTrace:{ex.StackTrace?.Substring(0, Math.Min(2000, ex.StackTrace.Length))} | Is Windows Package: {(App.IsWindowsPackage == true ? "True" : "False")}", Timezone = timeZone, Language = language }), Encoding.UTF8, "application/json");
                    await client.PostAsync(AnalyticsTargets.ER, jsonContent);
                }
                catch (Exception ex) { }
            });
        }
        public void SendCustomEvent(string eventName, object meta)
        {
            if (!trackingEnabled) return;
            Task.Run(async () =>
            {
                try
                {
                    var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData() { Event = eventName, Metadata = meta, Timezone = timeZone, Language = language }), Encoding.UTF8, "application/json");
                    await client.PostAsync(AnalyticsTargets.CU, jsonContent);
                }
                catch { }
            });
        }
        private string? ParseUserControl(UserControl page)
        {
            switch (page)
            {
                case LibraryUserControl:
                    {
                        return "/library";

                    }
                case DownloadsUserControl:
                    {
                        return "/downloads";

                    }
                case CommunityUserControl:
                    {
                        return "/community";

                    }
                case SettingsUserControl:
                    {
                        return "/settings";

                    }
                case AdminConsoleUserControl:
                    {
                        return "/admin";
                    }
                case Wizard:
                    {
                        return "/wizard";
                    }
                case GameViewUserControl:
                    {
                        return "/game";
                    }
                default:
                    {
                        return null;
                    }
            }
        }
        public object GetSysInfo()
        {
            try
            {
                var OS = new ManagementObjectSearcher("select * from Win32_OperatingSystem").Get().Cast<ManagementObject>().First();
                string os = $"{OS["Caption"]} - {OS["OSArchitecture"]} - Version.{OS["Version"]}"; os = os.Replace("NT 5.1.2600", "XP"); os = os.Replace("NT 5.2.3790", "Server 2003");
                string ram = $"{OS["TotalVisibleMemorySize"]} KB";
                var CPU = new ManagementObjectSearcher("select * from Win32_Processor").Get().Cast<ManagementObject>().First();
                string cpu = $"{CPU["Name"]} - {CPU["MaxClockSpeed"]} MHz - {CPU["NumberOfCores"]} Core";
                return new { app_version = SettingsViewModel.Instance.Version, hardware_os = os, hardware_ram = ram, hardware_cpu = cpu, };
            }
            catch (Exception ex)
            {
                return new { app_version = SettingsViewModel.Instance.Version, hardware_os = $"The system information could not be loaded due to an {ex.GetType().Name}" };
            }
        }
        private bool IsWineRunning()
        {
            // Search for WINLOGON process
            int p = Process.GetProcessesByName("winlogon").Length;
            return p == 0;
        }
        private class AnalyticsData
        {
            [JsonPropertyName("pid")]
            public string ProjectID => "N2kuL4i8qmOQ";

            [JsonPropertyName("ev")]
            public string? Event { get; set; }
            [JsonPropertyName("tz")]
            public string? Timezone { get; set; }
            [JsonPropertyName("pg")]
            public string? CurrentPage { get; set; }

            [JsonPropertyName("lc")]
            public string? Language { get; set; }
            [JsonPropertyName("meta")]
            public object? Metadata { get; set; }//Properties of type string only
            //Error
            [JsonPropertyName("name")]
            public string? ExceptionType { get; set; }
            [JsonPropertyName("message")]
            public string? ExceptionMessage { get; set; }
        }
        private static class AnalyticsTargets
        {
            public static string HB => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9hbmFseXRpY3MucGxhdGZvcm0ucGhhbGNvLmRlL2xvZy9oYg=="));
            public static string LG => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9hbmFseXRpY3MucGxhdGZvcm0ucGhhbGNvLmRlL2xvZw=="));
            public static string CU => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9hbmFseXRpY3MucGxhdGZvcm0ucGhhbGNvLmRlL2xvZy9jdXN0b20="));
            public static string ER => Encoding.UTF8.GetString(Convert.FromBase64String("aHR0cHM6Ly9hbmFseXRpY3MucGxhdGZvcm0ucGhhbGNvLmRlL2xvZy9lcnJvcg=="));
        }
    }
}
