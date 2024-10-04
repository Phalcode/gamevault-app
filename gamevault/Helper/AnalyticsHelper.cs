using AngleSharp.Io;
using gamevault.UserControls;
using gamevault.ViewModels;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private string IP = "";
        private string timeZone;
        private string language;
        private HttpClient client;
        private bool trackingEnabled = false;
        internal AnalyticsHelper()
        {
            trackingEnabled = SettingsViewModel.Instance.SendAnonymousAnalytics;
            if (!trackingEnabled)
                return;

            client = new HttpClient();
            try
            {
                IP = client.GetStringAsync("https://api.ipify.org").Result;
            }
            catch { }
            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Mozilla", "5.0"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(Windows NT 10.0; Win64; x64)"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AppleWebKit", "537.36"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(KHTML, like Gecko)"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Chrome", "129.0.0.0"));
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Safari", "537.36"));
            client.DefaultRequestHeaders.Add("X-Client-IP-Address", IP);
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
            SendHeartBeat("https://analytics.platform.phalco.de/log/hb");
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
                    await client.PostAsync("https://analytics.platform.phalco.de/log/custom", jsonContent);
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
            return string.Empty;
        }
        private async void SendHeartBeat(string url)
        {
            try
            {
                var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData()), Encoding.UTF8, "application/json");
                await client.PostAsync(url, jsonContent);
                //string responseBody = await response.Content.ReadAsStringAsync();
            }
            catch (Exception e) { }

        }
        public async void SendPageView(UserControl page, UserControl prevPage)
        {
            if (!trackingEnabled)
                return;

            try
            {
                string pageString = ParseUserControl(page);
                string prevPageString = ParseUserControl(prevPage);
                var jsonContent = new StringContent(JsonSerializer.Serialize(new AnalyticsData() { Timezone = timeZone, CurrentPage = pageString, PreviousPage = prevPageString, Language = language }), Encoding.UTF8, "application/json");
                await client.PostAsync("https://analytics.platform.phalco.de/log", jsonContent);
            }
            catch (Exception e) { }

        }
        private string ParseUserControl(UserControl page)
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
                        return "/unknown";
                    }
            }
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
            [JsonPropertyName("prev")]
            public string? PreviousPage { get; set; }
            [JsonPropertyName("lc")]
            public string? Language { get; set; }
            [JsonPropertyName("meta")]
            public object? Metadata { get; set; }//Properties of type string only
        }
    }
}
