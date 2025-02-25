using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class MinimalGame
    {
        /// <summary>
        /// slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.
        /// </summary>
        /// <value>slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.</value>

        [JsonPropertyName("provider_slug")]
        public string ProviderSlug { get; set; }

        /// <summary>
        /// id of the game from the provider
        /// </summary>
        /// <value>id of the game from the provider</value>

        [JsonPropertyName("provider_data_id")]
        public string ProviderDataId { get; set; }

        /// <summary>
        /// gamevault's calculated probability of the metadata being the correct one.
        /// </summary>
        /// <value>gamevault's calculated probability of the metadata being the correct one.</value>

        [JsonPropertyName("provider_probability")]
        public decimal? ProviderProbability { get; set; }

        /// <summary>
        /// title of the game
        /// </summary>
        /// <value>title of the game</value>

        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// release date of the game
        /// </summary>
        /// <value>release date of the game</value>

        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// box image url of the game
        /// </summary>
        /// <value>box image url of the game</value>

        [JsonPropertyName("cover_url")]
        public string CoverUrl { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }
}
