using crackpipe.Helper;
using crackpipe.Models;
using crackpipe.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace crackpipe.UserControls
{
    /// <summary>
    /// Interaction logic for UserEditUserControl.xaml
    /// </summary>
    public partial class UserEditUserControl : UserControl
    {
        internal event EventHandler UserSaved;
        internal UserEditUserControl(User user)
        {
            InitializeComponent();

            this.DataContext = Copy(user);

        }

        private void Save_Clicked(object sender, RoutedEventArgs e)
        {
            if (this.UserSaved != null)
                this.UserSaved(sender, e);            
        }
        private User Copy(User source)
        {
            User dest = new User();
            foreach (var prop in dest.GetType().GetProperties())
            {
                prop.SetValue(dest, prop.GetValue(source));
            }
            return dest;
        }
    }
}
