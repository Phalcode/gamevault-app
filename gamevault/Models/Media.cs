using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;


namespace gamevault.Models
{
    public class Media
    {
        /// <summary>
        /// Unique gamevault-identifier of the entity
        /// </summary>
        /// <value>Unique gamevault-identifier of the entity</value>

        [JsonPropertyName("id")]
        public int ID { get; set; }

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
        /// the original source URL of the media
        /// </summary>
        /// <value>the original source URL of the media</value>

        [JsonPropertyName("source_url")]
        public string Source { get; set; }

        /// <summary>
        /// the path of the media on the filesystem
        /// </summary>
        /// <value>the path of the media on the filesystem</value>

        [JsonPropertyName("file_path")]
        public string Path { get; set; }

        /// <summary>
        /// the media type of the media on the filesystem
        /// </summary>
        /// <value>the media type of the media on the filesystem</value>

        [JsonPropertyName("type")]
        public string Type { get; set; }

        /// <summary>
        /// the uploader of the media
        /// </summary>
        /// <value>the uploader of the media</value>

        [JsonPropertyName("uploader")]
        public User Uploader { get; set; }
    }

}