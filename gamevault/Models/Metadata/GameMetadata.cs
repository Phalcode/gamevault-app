using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class GameMetadata
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
        /// games the metadata belongs to
        /// </summary>
        /// <value>games the metadata belongs to</value>

        [JsonPropertyName("gamevault_games")]
        public List<Game>? GamevaultGames { get; set; }

        /// <summary>
        /// slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.
        /// </summary>
        /// <value>slug (url-friendly name) of the provider. This is the primary identifier. Must be formatted like a valid slug.</value>

        [JsonPropertyName("provider_slug")]
        public string? ProviderSlug { get; set; }


        [JsonPropertyName("provider_priority")]
        public int? ProviderPriority { get; set; }

        /// <summary>
        /// id of the game from the provider
        /// </summary>
        /// <value>id of the game from the provider</value>

        [JsonPropertyName("provider_data_id")]
        public string? ProviderDataId { get; set; }

        /// <summary>
        /// gamevault's calculated probability of the metadata being the correct one.
        /// </summary>
        /// <value>gamevault's calculated probability of the metadata being the correct one.</value>

        [JsonPropertyName("provider_probability")]
        public decimal? ProviderProbability { get; set; }

        /// <summary>
        /// checksum of the provider data
        /// </summary>
        /// <value>checksum of the provider data</value>

        [JsonPropertyName("provider_checksum")]
        public string? ProviderChecksum { get; set; }

        /// <summary>
        /// the minimum age required to play the game
        /// </summary>
        /// <value>the minimum age required to play the game</value>

        [JsonPropertyName("age_rating")]
        public int? AgeRating { get; set; }

        /// <summary>
        /// title of the game
        /// </summary>
        /// <value>title of the game</value>

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        /// <summary>
        /// release date of the game
        /// </summary>
        /// <value>release date of the game</value>

        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// description of the game
        /// </summary>
        /// <value>description of the game</value>

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// average playtime of other people in the game in minutes
        /// </summary>
        /// <value>average playtime of other people in the game in minutes</value>

        [JsonPropertyName("average_playtime")]
        public int? AveragePlaytime { get; set; }

        /// <summary>
        /// cover/boxart image of the game
        /// </summary>
        /// <value>cover/boxart image of the game</value>

        [JsonPropertyName("cover")]
        public Media? Cover { get; set; }

        /// <summary>
        /// background image of the game
        /// </summary>
        /// <value>background image of the game</value>

        [JsonPropertyName("background")]
        public Media? Background { get; set; }

        /// <summary>
        /// screenshots of the game
        /// </summary>
        /// <value>screenshots of the game</value>

        [JsonPropertyName("url_screenshots")]
        public string[]? Screenshots { get; set; }
        public string ScreenshotNames
        {
            get
            {
                if (Screenshots == null)
                {
                    return string.Empty;
                }
                return string.Join(",", Screenshots);
            }
        }

        [JsonPropertyName("url_trailers")]
        public string[]? Trailers { get; set; }
        public string TrailerNames
        {
            get
            {
                if (Trailers == null)
                {
                    return string.Empty;
                }
                return string.Join(",", Trailers);
            }
        }
        [JsonPropertyName("url_gameplays")]
        public string[]? Gameplays { get; set; }
        public string GameplayNames
        {
            get
            {
                if (Gameplays == null)
                {
                    return string.Empty;
                }
                return string.Join(",", Gameplays);
            }
        }
        /// <summary>
        /// website url of the game
        /// </summary>
        /// <value>website url of the game</value>

        [JsonPropertyName("url_websites")]
        public string[]? UrlWebsites { get; set; }
        public string WebsiteNames
        {
            get
            {
                if (UrlWebsites == null)
                {
                    return string.Empty;
                }
                return string.Join(",", UrlWebsites);
            }
        }

        private decimal? rating { get; set; }
        /// <summary>
        /// rating of the provider
        /// </summary>
        /// <value>rating of the provider</value>

        [JsonPropertyName("rating")]
        public decimal? Rating
        {
            get { return rating.HasValue ? Math.Round(rating.Value, 2) : null; }
            set { rating = value; }
        }

        /// <summary>
        /// indicates if the game is in early access
        /// </summary>
        /// <value>indicates if the game is in early access</value>

        [JsonPropertyName("early_access")]
        public bool? EarlyAccess { get; set; }

        /// <summary>
        /// publishers of the game
        /// </summary>
        /// <value>publishers of the game</value>

        [JsonPropertyName("publishers")]
        public List<PublisherMetadata>? Publishers { get; set; }
        public string PublisherNames
        {
            get
            {
                if (Publishers == null)
                    return string.Empty;

                return string.Join(",", Publishers.Select(publisher => publisher.Name));
            }
        }
        /// <summary>
        /// developers of the game
        /// </summary>
        /// <value>developers of the game</value>

        [JsonPropertyName("developers")]
        public List<DeveloperMetadata>? Developers { get; set; }
        public string DeveloperNames
        {
            get
            {
                if (Developers == null)
                    return string.Empty;

                return string.Join(",", Developers.Select(developer => developer.Name));
            }
        }

        /// <summary>
        /// tags of the game
        /// </summary>
        /// <value>tags of the game</value>

        [JsonPropertyName("tags")]
        public List<TagMetadata>? Tags { get; set; }
        public string TagNames
        {
            get
            {
                if (Tags == null)
                    return string.Empty;

                return string.Join(",", Tags.Select(tag => tag.Name));
            }
        }

        /// <summary>
        /// genres of the game
        /// </summary>
        /// <value>genres of the game</value>

        [JsonPropertyName("genres")]
        public List<GenreMetadata>? Genres { get; set; }
        public string GenreNames
        {
            get
            {
                if (Genres == null)
                    return string.Empty;

                return string.Join(",", Genres.Select(genre => genre.Name));
            }
        }

        /// <summary>
        /// public notes from the admin for the game. markdown supported.
        /// </summary>
        /// <value>public notes from the admin for the game. markdown supported.</value>

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("launch_executable")]
        public string? LaunchExecutable { get; set; }

        [JsonPropertyName("launch_parameters")]
        public string? LaunchParameters { get; set; }

        [JsonPropertyName("installer_parameters")]
        public string? InstallerParameters { get; set; }

        [JsonPropertyName("installer_executable")]
        public string? InstallerExecutable { get; set; }

        [JsonPropertyName("uninstaller_parameters")]
        public string? UninstallerParameters { get; set; }

        [JsonPropertyName("uninstaller_executable")]
        public string? UninstallerExecutable { get; set; }
    }
}
