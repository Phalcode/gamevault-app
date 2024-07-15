using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class Pill : IGenreMetadata, ITagMetadata
    {
        private int id;
        private string name;
        private string providerDataId;
        private string providerSlug;
        [JsonPropertyName("id")]
        public int ID { get => id; set => id = value; }
        [JsonPropertyName("name")]
        public string Name { get => name; set => name = value; }
        [JsonPropertyName("provider_data_id")]
        public string ProviderDataId { get => providerDataId; set => providerDataId = value; }
        [JsonPropertyName("provider_slug")]
        public string ProviderSlug { get => providerSlug; set => providerSlug = value; }

        public string OriginName { get; set; }
    }
}
