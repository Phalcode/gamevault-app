using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace gamevault.Models
{
    public enum GameType
    {
        UNDETECTABLE,
        [Description("🖥⚙️ Windows Setup")]
        WINDOWS_SETUP,
        [Description("🖥🎮 Windows Portable")]
        WINDOWS_PORTABLE,
        [Description("🐧🎮 Linux Portable")]
        LINUX_PORTABLE
    }
    public class Game
    {

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
        /// file path to the game or the game manifest (relative to root)
        /// </summary>
        /// <value>file path to the game or the game manifest (relative to root)</value>

        [JsonPropertyName("file_path")]
        public string Path { get; set; }

        /// <summary>
        /// size of the game file in bytes
        /// </summary>
        /// <value>size of the game file in bytes</value>

        [JsonPropertyName("size")]
        public string Size { get; set; }

        /// <summary>
        /// title of the game (extracted from the filename')
        /// </summary>
        /// <value>title of the game (extracted from the filename')</value>

        [JsonPropertyName("title")]
        public string Title { get; set; }

        /// <summary>
        /// Title of the game used to sort collection eg Halo 1, Borderlands 1.5, etc.
        /// </summary>
        /// <value>title of the game used to sort collection eg Halo 1, Borderlands 1.5, etc.</value>

        [JsonPropertyName("sort_title")]
        public string? SortTitle { get; set; }

        /// <summary>
        /// version tag (extracted from the filename e.g. '(v1.0.0)')
        /// </summary>
        /// <value>version tag (extracted from the filename e.g. '(v1.0.0)')</value>

        [JsonPropertyName("version")]
        public string Version { get; set; }

        /// <summary>
        /// release date of the game (extracted from filename e.g. '(2013)')
        /// </summary>
        /// <value>release date of the game (extracted from filename e.g. '(2013)')</value>

        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// indicates if the game is an early access title (extracted from filename e.g. '(EA)')
        /// </summary>
        /// <value>indicates if the game is an early access title (extracted from filename e.g. '(EA)')</value>

        [JsonPropertyName("early_access")]
        public bool? EarlyAccess { get; set; }

        /// <summary>
        /// type of the game, see https://gamevau.lt/docs/server-docs/game-types for all possible values
        /// </summary>
        /// <value>type of the game, see https://gamevau.lt/docs/server-docs/game-types for all possible values</value>

        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType Type { get; set; }

        /// <summary>
        /// metadata of various providers associated to the game
        /// </summary>
        /// <value>metadata of various providers associated to the game</value>

        [JsonPropertyName("provider_metadata")]
        public List<GameMetadata> ProviderMetadata { get; set; }

        /// <summary>
        /// user-defined metadata of the game
        /// </summary>
        /// <value>user-defined metadata of the game</value>

        [JsonPropertyName("user_metadata")]
        public GameMetadata? UserMetadata { get; set; }

        /// <summary>
        /// effective and merged metadata of the game
        /// </summary>
        /// <value>effective and merged metadata of the game</value>

        [JsonPropertyName("metadata")]
        public GameMetadata? Metadata { get; set; }

        /// <summary>
        /// progresses associated to the game
        /// </summary>
        /// <value>progresses associated to the game</value>

        [JsonPropertyName("progresses")]
        public List<Progress> Progresses { get; set; }

        /// <summary>
        /// users that bookmarked this game
        /// </summary>
        /// <value>users that bookmarked this game</value>

        [JsonPropertyName("bookmarked_users")]
        public List<User> BookmarkedUsers { get; set; }

        /// <summary>
        /// count of downloads on this server
        /// </summary>     

        [JsonPropertyName("download_count")]
        public int DownloadCount { get; set; }
    }
}
