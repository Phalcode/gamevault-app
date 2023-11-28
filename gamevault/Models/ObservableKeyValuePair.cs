using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gamevault.Models
{
    internal class ObservableKeyValuePair : ViewModelBase
    {
        public ObservableKeyValuePair(Game g, string v)
        {
            Key = g;
            Value = v;
        }
        private Game key { get; set; }
        private string val { get; set; }

        public Game Key
        {
            get { return key; }
            set { key = value; OnPropertyChanged(); }
        }

        public string Value
        {
            get { return val; }
            set { val = value; OnPropertyChanged(); }
        }
    }
}
