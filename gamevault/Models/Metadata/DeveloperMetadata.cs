using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace gamevault.Models
{
    public class DeveloperMetadata
    {
        /// <summary>
        /// Unique gamevault-identifier of the entity
        /// </summary>
        /// <value>Unique gamevault-identifier of the entity</value>

        [JsonPropertyName("id")]
        public int? ID { get; set; }

        /// <summary>
        /// date the entity was created
        /// </summary>
        /// <value>date the entity was created</value>

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// date the entity was updated
        /// </summary>
        /// <value>date the entity was updated</value>

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// date the entity was soft-deleted (null if not deleted)
        /// </summary>
        /// <value>date the entity was soft-deleted (null if not deleted)</value>

        [JsonPropertyName("deleted_at")]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// incremental version number of the entity
        /// </summary>
        /// <value>incremental version number of the entity</value>

        [JsonPropertyName("entity_version")]
        public decimal? EntityVersion { get; set; }

        /// <summary>
        /// slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.
        /// </summary>
        /// <value>slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.</value>

        [JsonPropertyName("provider_slug")]
        public string ProviderSlug { get; set; }

        /// <summary>
        /// id of the developer from the provider
        /// </summary>
        /// <value>id of the developer from the provider</value>

        [JsonPropertyName("provider_data_id")]
        public string ProviderDataId { get; set; }

        /// <summary>
        /// name of the developer
        /// </summary>
        /// <value>name of the developer</value>

        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// games developed by the developer
        /// </summary>
        /// <value>games developed by the developer</value>

        [JsonPropertyName("games")]
        public List<GameMetadata> Games { get; set; }


    }
}
