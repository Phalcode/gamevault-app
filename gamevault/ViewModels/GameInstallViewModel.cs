using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace gamevault.ViewModels
{
    internal class GameInstallViewModel : ViewModelBase
    {
        #region PrivateMembers
        private Game m_Game { get; set; }
        private ObservableCollection<KeyValuePair<string, string>> m_Executables { get; set; }

        #endregion

        public Game Game
        {
            get { return m_Game; }
            set { m_Game = value; OnPropertyChanged(); }
        }
        public ObservableCollection<KeyValuePair<string, string>> Executables
        {
            get
            {
                if (m_Executables == null)
                {
                    m_Executables = new ObservableCollection<KeyValuePair<string, string>>();
                }
                return m_Executables;
            }
            set { m_Executables = value; OnPropertyChanged(); }
        }
    }
}
