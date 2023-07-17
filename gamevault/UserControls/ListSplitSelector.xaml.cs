using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace gamevault.UserControls
{
    /// <summary>
    /// Interaction logic for ListSplitSelector.xaml
    /// </summary>
    public partial class ListSplitSelector : UserControl
    {
        public static readonly DependencyProperty UsageProperty = DependencyProperty.Register(name: "Usage", propertyType: typeof(string), ownerType: typeof(ListSplitSelector), typeMetadata: new FrameworkPropertyMetadata(defaultValue: string.Empty));
        public string Usage
        {
            get => (string)GetValue(UsageProperty);
            set => SetValue(UsageProperty, value);
        }

        private Genre_Tag[] m_Data { get; set; }
        private List<Genre_Tag> m_SelectedData = new List<Genre_Tag>();
        private System.Timers.Timer m_DebounceTimer { get; set; }
        private string SearchText { get; set; }
        public ListSplitSelector()
        {
            InitializeComponent();
            m_DebounceTimer = new System.Timers.Timer();
            m_DebounceTimer.AutoReset = false;
            m_DebounceTimer.Interval = 1500;
            m_DebounceTimer.Elapsed += Timer_Elapsed;
        }
        public string GetSelectedValues()
        {
            return string.Join(",", m_SelectedData.Select(o => o.Name));
        }
        private async void LoadValues_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (m_Data == null)
                {
                    string url = string.Empty;
                    string usage = Usage;
                    if (usage == "0")
                    {
                        url = $"{SettingsViewModel.Instance.ServerUrl}/api/v1/genres";
                    }
                    else if (usage == "1")
                    {
                        url = $"{SettingsViewModel.Instance.ServerUrl}/api/v1/tags?limit=100";
                    }
                    await Task.Run(() =>
                    {
                        string result = WebHelper.GetRequest(url);
                        if (usage == "0")
                        {
                            m_Data = JsonSerializer.Deserialize<Genre_Tag[]>(result);
                        }
                        else if (usage == "1")
                        {
                            m_Data = JsonSerializer.Deserialize<PaginatedData<Genre_Tag>>(result).Data;
                        }

                    });
                    uiItemControl.ItemsSource = m_Data;
                    (uiValueScrollViewer.Template.FindName("PART_HorizontalScrollBar", uiValueScrollViewer) as ScrollBar).Height = 7;
                }
                uiGenrePopup.IsOpen = true;
            }
            catch (Exception ex)
            {
            }
        }

        private void Input_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Usage == "0")
            {
                if (m_Data != null)
                {
                    string text = ((TextBox)sender).Text;
                    if (text != string.Empty)
                    {
                        uiItemControl.ItemsSource = m_Data.Where(x => x.Name.IndexOf(text, 0, StringComparison.OrdinalIgnoreCase) != -1);
                    }
                    else
                    {
                        uiItemControl.ItemsSource = m_Data;
                    }
                }
            }
            else if (Usage == "1")
            {
                m_DebounceTimer.Stop();
                m_DebounceTimer.Start();
                SearchText = ((TextBox)sender).Text;
            }
        }
        private async void Timer_Elapsed(object sender, EventArgs e)
        {
            string url = string.Empty;
            url = $"{SettingsViewModel.Instance.ServerUrl}/api/v1/tags?search={SearchText}&limit=100";
            await Task.Run(() =>
            {
                string result = WebHelper.GetRequest(url);
                m_Data = JsonSerializer.Deserialize<PaginatedData<Genre_Tag>>(result).Data;
            });
            Dispatcher.Invoke(() => { uiItemControl.ItemsSource = m_Data; });
        }

        private void ItemSelected_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Genre_Tag genre = (Genre_Tag)((FrameworkElement)sender).DataContext;
            if (m_SelectedData.Where(x => x.ID == genre.ID).Count() == 0)
            {
                uiSelectedItemControl.ItemsSource = null;
                m_SelectedData.Add(genre);
                uiSelectedItemControl.ItemsSource = m_SelectedData;
            }
        }

        private void ValuesDelete_Click(object sender, MouseButtonEventArgs e)
        {
            m_SelectedData.Clear();
            uiSelectedItemControl.ItemsSource = null;
            uiSelectedItemControl.ItemsSource = m_SelectedData;
        }
    }
}
