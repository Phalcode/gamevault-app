using gamevault.Models;
using gamevault.Models.Mapping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace gamevault.Converter
{
    internal class IsGameMappedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (values[1].GetType() == typeof(GameMetadata))
                {
                    return ((List<GameMetadata>)values[0]).Any(entry => entry.ProviderSlug == ((GameMetadata)values[1]).ProviderSlug);
                }
                else if(values[1].GetType() == typeof(MetadataProviderDto))
                {
                    return ((List<GameMetadata>)values[0]).Any(entry => entry.ProviderSlug == ((MetadataProviderDto)values[1]).Slug);
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
