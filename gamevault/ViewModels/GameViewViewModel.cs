using gamevault.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class GameViewViewModel : ViewModelBase
    {
        #region PrivateMembers
        private Game? game { get; set; }
        private Progress? progress { get; set; }
        private Progress[]? userProgress { get; set; }
        private Dictionary<string, string> gameStates { get; set; }
        private bool isInstalled { get; set; }
        private bool? isDownloaded { get; set; }
        private bool showRawgTitle { get; set; }
        #endregion
        public Game? Game
        {
            get { return game; }
            set { game = value; OnPropertyChanged(); }
        }
        public Progress? Progress
        {
            get { return progress; }
            set { progress = value; OnPropertyChanged(); }
        }
        public Progress[]? UserProgress
        {
            get { return userProgress; }
            set { userProgress = value; OnPropertyChanged(); }
        }
        public Dictionary<string, string>? GameStates
        {
            get => gameStates ?? (gameStates = Enum.GetValues(typeof(State))
                .Cast<State>()
                .ToDictionary(state => GetEnumDescription(state), state => state.ToString()));
            set { gameStates = value; OnPropertyChanged(); }
        }

        private static string GetEnumDescription(State value) =>
            (Attribute.GetCustomAttribute(value.GetType().GetField(value.ToString()), typeof(DescriptionAttribute)) is DescriptionAttribute attribute) ? attribute.Description : value.ToString();
        public bool IsInstalled
        {
            get { return isInstalled; }
            set { isInstalled = value; OnPropertyChanged(); }
        }
        public bool? IsDownloaded
        {
            get { return isDownloaded; }
            set { isDownloaded = value; OnPropertyChanged(); }
        }
        public bool ShowRawgTitle
        {
            get { return showRawgTitle; }
            set { showRawgTitle = value; OnPropertyChanged(); }
        }
    }
}
