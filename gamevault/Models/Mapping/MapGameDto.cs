using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models.Mapping
{
    public class MapGameDto
    {
        /// slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.
        [JsonPropertyName("provider_slug")]
        public string ProviderSlug { get; set; }

        /// id of the target game from the provider. If not provided, the metadata for the specified provider will be unmapped.
        [JsonPropertyName("provider_data_id")]
        public string ProviderDataId { get; set; }

        /// opional priority override of the provider for the specified game. If not provided, the default priority of the provider will be used.
        [JsonPropertyName("provider_priority")]
        public int? ProviderPriority { get; set; }
    }
}
