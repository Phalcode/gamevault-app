using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class GameSettingsViewModel : ViewModelBase
    {
        #region Privates

        private Game game { get; set; }
        private string directory { get; set; }
        private ObservableCollection<KeyValuePair<string, string>> m_Executables { get; set; }
        private string launchParameter { get; set; }
        private RawgGame[]? rawgGames { get; set; }
        #endregion      
        public Game Game
        {
            get { return game; }
            set { game = value; OnPropertyChanged(); }
        }
        public string Directory
        {
            get { return directory; }
            set { directory = value; OnPropertyChanged(); }
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
        public string LaunchParameter
        {
            get { return launchParameter; }
            set { launchParameter = value; OnPropertyChanged(); }
        }
        public RawgGame[]? RawgGames
        {
            get { return rawgGames; }
            set { rawgGames = value; OnPropertyChanged(); }
        }
    }
}
