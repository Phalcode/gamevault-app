using gamevault.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.ViewModels
{
    internal class NewGameViewViewModel : ViewModelBase
    {
        #region PrivateMembers
        private Game? game { get; set; }
        private Progress? progress { get; set; }
        private Progress[]? userProgress { get; set; }
        private string[]? gameStates { get; set; }
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
        public string[]? GameStates
        {
            get { if (gameStates == null) { gameStates = Enum.GetNames(typeof(State)); } return gameStates; }
            set { gameStates = value; OnPropertyChanged(); }
        }
    }
}
