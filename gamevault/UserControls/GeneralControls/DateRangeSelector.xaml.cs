using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// Interaction logic for DateRangeSelector.xaml
    /// </summary>
    public partial class DateRangeSelector : UserControl
    {
        public string YearToPlaceholder { get; set; }
        public string YearFromPlaceholder { get; set; } = "1980";
        public event EventHandler EntriesUpdated;
        public DateRangeSelector()
        {
            InitializeComponent();
            YearToPlaceholder = DateTime.Now.Year.ToString();
            this.DataContext = this;
        }
        public bool IsValid()
        {
            return uiFilterYearFrom.Text != string.Empty && uiFilterYearTo.Text != string.Empty;
        }
        public string GetYearFrom()
        {
            return uiFilterYearFrom.Text;
        }
        public string GetYearTo()
        {
            return uiFilterYearTo.Text;
        }
        private void StackPanel_LostFocus(object sender, RoutedEventArgs e)
        {
            if(EntriesUpdated!=null && IsValid())
            EntriesUpdated(this, e);
        }
        private void YearSelector_Changed(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = (((TextBox)e.Source).Text == "" && e.Text == "0") || regex.IsMatch(e.Text);
        }
    }
}
