using crackpipe.Models;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace crackpipe.Converter
{
    class GameStateColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                State state = (State)Enum.Parse(typeof(State), value.ToString());
                switch(state)
                {
                    case State.UNPLAYED:
                        return Brushes.DarkGray;
                    case State.INFINITE:
                        return Brushes.Yellow;
                    case State.PLAYING:
                        return Brushes.Green;
                    case State.COMPLETED:
                        return Brushes.Violet;
                    case State.ABORTED_TEMPORARY:
                        return Brushes.Orange;
                    case State.ABORTED_PERMANENT:
                        return Brushes.Red;
                    default:
                        return null;
                }
                
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
