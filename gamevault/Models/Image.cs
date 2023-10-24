using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class Image
    {
        private string m_Path { get; set; }
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("source")]
        public string Source { get; set; }
        //[JsonPropertyName("path")]
        public string Path
        {
            get
            {
                //if (ID != null)
                //{
                //    return $"{SettingsViewModel.Instance.ServerUrl}/api/images/{ID}";
                //}
                //else
                //{
                    return m_Path ;
                //}
                //if (Uri.IsWellFormedUriString(m_Path, UriKind.Absolute))
                //{
                //    return m_Path;
                //}
                //else if (m_Path == string.Empty || m_Path == null)
                //{
                //    return string.Empty;
                //}
                //else
                //{
                //    return $"{SettingsViewModel.Instance.ServerUrl}/api/v1{m_Path}";
                //}
            }
            set { m_Path = value; }
        }

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; }

    }
}
