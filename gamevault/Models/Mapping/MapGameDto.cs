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
        /// <summary>
        /// slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.
        /// </summary>
        /// <value>slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.</value>
        [JsonPropertyName("provider_slug")]
        public string? ProviderSlug { get; set; }

        /// <summary>
        /// id of the target game from the provider. If not provided, the metadata for the specified provider will be unmapped.
        /// </summary>
        /// <value>id of the target game from the provider. If not provided, the metadata for the specified provider will be unmapped.</value>      
        [JsonPropertyName("target_provider_data_id")]
        public string? TargetProviderDataId { get; set; }
    }
}
