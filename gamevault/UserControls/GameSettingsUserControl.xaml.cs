using gamevault.Helper;
using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaction logic for GameSettingsUserControl.xaml
    /// </summary>
    public partial class GameSettingsUserControl : UserControl
    {
        private bool startup = true;
        public GameSettingsUserControl()
        {
            InitializeComponent();
        }
        private void SettingsTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == -1)
                return;

            if (sender == uiSettingsHeadersLocal)
            {
                uiSettingsHeadersRemote.SelectedIndex = -1;
                uiSettingsContent.SelectedIndex = uiSettingsHeadersLocal.SelectedIndex;
            }
            else if (sender == uiSettingsHeadersRemote)
            {
                if (startup)
                {
                    startup = false;
                    uiSettingsHeadersRemote.SelectedIndex = -1;
                }
                else
                {
                    uiSettingsHeadersLocal.SelectedIndex = -1;
                    uiSettingsContent.SelectedIndex = uiSettingsHeadersRemote.SelectedIndex + uiSettingsHeadersLocal.Items.Count;
                }
            }
        }

        private void Close_Click(object sender, MouseButtonEventArgs e)
        {
            var parent = VisualHelper.FindNextParentByType<GameSettingsUserControl>(((FrameworkElement)sender)).Parent;
            if (parent.GetType() == typeof(Popup))
            {
                ((Popup)parent).IsOpen = false;
            }
        }
    }
}
