using gamevault.Converter;
using gamevault.Helper;
using gamevault.Models;
using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace gamevault.UserControls
{
    public enum Selection
    {
        Tags,
        Genres,
        GameType
    }
    public partial class PillSelector : UserControl
    {
        public static readonly DependencyProperty SelectionTypeProperty = DependencyProperty.Register(name: "SelectionType", propertyType: typeof(Selection), ownerType: typeof(PillSelector));
        public Selection SelectionType
        {
            get => (Selection)GetValue(SelectionTypeProperty);
            set => SetValue(SelectionTypeProperty, value);
        }
        public event EventHandler EntriesUpdated;
        private bool loaded = false;
        private InputTimer debounceTimer { get; set; }
        private List<Pill> selectedEntries = new List<Pill>();
        public PillSelector()
        {
            InitializeComponent();
        }
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded) return;
            loaded = true;
            uiTxtHeader.Text = SelectionType switch //C# 8.0 more compact switch case
            {
                Selection.Tags => "Tags",
                Selection.Genres => "Genres",
                Selection.GameType => "Game Type",
                _ => uiTxtHeader.Text
            };
            InitTimer();
            await LoadSelectionEntries();
        }
        private void InitTimer()
        {
            debounceTimer = new InputTimer() { Data = string.Empty };
            debounceTimer.Interval = TimeSpan.FromMilliseconds(400);
            debounceTimer.Tick += DebounceTimerElapsed;
        }
        public string GetSelectedEntries()
        {
            if (SelectionType == Selection.GameType)
                return string.Join(",", selectedEntries.Select(o => o.OriginName));

            return string.Join(",", selectedEntries.Select(o => o.Name));
        }
        public bool HasEntries()
        {
            return selectedEntries.Any();
        }
        public void ClearEntries()
        {
            selectedEntries.Clear();
            uiSelectedEntries.ItemsSource = null;
        }
        public void SetEntries(Pill[] data)
        {
            ClearEntries();
            foreach (Pill dataEntry in data)
            {
                selectedEntries.Add(dataEntry);
            }
            uiSelectedEntries.ItemsSource = selectedEntries;
            if (EntriesUpdated != null)
            {
                EntriesUpdated(this, null);
            }
        }
        private async void DebounceTimerElapsed(object? sender, EventArgs e)
        {
            debounceTimer.Stop();
            await LoadSelectionEntries();
        }
        private async Task LoadSelectionEntries()
        {
            Pill[] data = null;
            if (SelectionType != Selection.GameType)
            {
                string url = string.Empty;
                url = SelectionType switch
                {
                    Selection.Tags => $"{SettingsViewModel.Instance.ServerUrl}/api/tags?search={debounceTimer.Data}&limit=25",
                    Selection.Genres => $"{SettingsViewModel.Instance.ServerUrl}/api/genres?search={debounceTimer.Data}&limit=25"
                };

                Selection selection = SelectionType;
                await Task.Run(() =>
                {
                    try
                    {
                        string result = WebHelper.GetRequest(url);
                        data = selection switch
                        {
                            Selection.Tags => JsonSerializer.Deserialize<PaginatedData<Pill>>(result).Data,
                            Selection.Genres => JsonSerializer.Deserialize<PaginatedData<Pill>>(result).Data//.Where(x => x.Name.Contains(debounceTimer.Data, StringComparison.OrdinalIgnoreCase)).ToArray()
                        };
                    }
                    catch (Exception ex)
                    {
                        MainWindowViewModel.Instance.AppBarText = WebExceptionHelper.TryGetServerMessage(ex);
                    }
                });
            }
            else
            {
                EnumDescriptionConverter conv = new EnumDescriptionConverter();
                List<Pill> list = new List<Pill>();
                foreach (GameType type in Enum.GetValues(typeof(GameType)))
                {
                    if (type == GameType.UNDETECTABLE)
                        continue;

                    list.Add(new Pill() { OriginName = type.ToString(), Name = (string)conv.Convert(type, null, null, null) });
                }
                data = list.ToArray();
                data = data.Where(x => x.Name.Contains(debounceTimer.Data, StringComparison.OrdinalIgnoreCase)).ToArray();
            }
            uiSelectionEntries.ItemsSource = data;
        }
        private void OpenSelection_Click(object sender, MouseButtonEventArgs e)
        {
            uiTxtSelectionHeader.Text = SelectionType switch //C# 8.0 more compact switch case
            {
                Selection.Tags => "Add Tags",
                Selection.Genres => "Add Genres",
                Selection.GameType => "Add Game Type",
                _ => uiTxtHeader.Text
            };
            uiSelectionpopup.IsOpen = true;
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            debounceTimer.Stop();
            debounceTimer.Data = ((TextBox)sender).Text;
            debounceTimer.Start();
        }

        private void AddEntry_Click(object sender, MouseButtonEventArgs e)
        {
            if (selectedEntries.Contains((Pill)((FrameworkElement)sender).DataContext)) return;
            selectedEntries.Add((Pill)((FrameworkElement)sender).DataContext);
            uiSelectedEntries.ItemsSource = null;
            uiSelectedEntries.ItemsSource = selectedEntries;
            if (EntriesUpdated != null)
            {
                EntriesUpdated(this, e);
            }
        }

        private void RemoveEntry_Click(object sender, MouseButtonEventArgs e)
        {
            selectedEntries.Remove((Pill)((FrameworkElement)sender).DataContext);
            uiSelectedEntries.ItemsSource = null;
            uiSelectedEntries.ItemsSource = selectedEntries;
            if (EntriesUpdated != null)
            {
                EntriesUpdated(this, e);
            }
        }

    }
}
