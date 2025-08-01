using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using gamevault.Models;

namespace IO.Swagger.Model
{
    public class UpdateGameUserMetadataDto
    {
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
        /// Title of the game used to sort collection eg Halo 1, Borderlands 1.5, etc.
        /// </summary>
        /// <value>title of the game used to sort collection eg Halo 1, Borderlands 1.5, etc.</value>

        [JsonPropertyName("sort_title")]
        public string? SortTitle { get; set; }

        /// <summary>
        /// release date of the game as ISO8601 string
        /// </summary>
        /// <value>release date of the game as ISO8601 string</value>

        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }

        /// <summary>
        /// description of the game. markdown supported.
        /// </summary>
        /// <value>description of the game. markdown supported.</value>
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }

        /// <summary>
        /// public notes from the admin for the game. markdown supported.
        /// </summary>
        /// <value>public notes from the admin for the game. markdown supported.</value>
        
        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// average playtime of other people in the game in minutes
        /// </summary>
        /// <value>average playtime of other people in the game in minutes</value>
        
        [JsonPropertyName("average_playtime")]
        public decimal? AveragePlaytime { get; set; }

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
        /// Predefined launch parameters for the game.
        /// </summary>
        /// <value>Predefined launch parameters for the game.</value>
        
        [JsonPropertyName("launch_parameters")]
        public string? LaunchParameters { get; set; }

        /// <summary>
        /// Predefined launch executable for the game.
        /// </summary>
        /// <value>Predefined launch executable for the game.</value>
        
        [JsonPropertyName("launch_executable")]
        public string? LaunchExecutable { get; set; }

       
        [JsonPropertyName("installer_parameters")]
        public string? InstallerParameters { get; set; }

        [JsonPropertyName("installer_executable")]
        public string? InstallerExecutable { get; set; }

       
        [JsonPropertyName("uninstaller_parameters")]
        public string? UninstallerParameters { get; set; }

       
        [JsonPropertyName("uninstaller_executable")]
        public string? UninstallerExecutable { get; set; }

        /// <summary>
        /// URLs of externally hosted screenshots of the game
        /// </summary>
        /// <value>URLs of externally hosted screenshots of the game</value>

        [JsonPropertyName("url_screenshots")]
        public string[]? UrlScreenshots { get; set; }

        /// <summary>
        /// URLs of externally hosted trailer videos of the game
        /// </summary>
        /// <value>URLs of externally hosted trailer videos of the game</value>
        
        [JsonPropertyName("url_trailers")]
        public string[]? UrlTrailers { get; set; }

        /// <summary>
        /// URLs of externally hosted gameplay videos of the game
        /// </summary>
        /// <value>URLs of externally hosted gameplay videos of the game</value>
        
        [JsonPropertyName("url_gameplays")]
        public string[]? UrlGameplays { get; set; }

        /// <summary>
        /// URLs of websites of the game
        /// </summary>
        /// <value>URLs of websites of the game</value>
        
        [JsonPropertyName("url_websites")]
        public string[]? UrlWebsites { get; set; }

        /// <summary>
        /// publishers of the game
        /// </summary>
        /// <value>publishers of the game</value>
        
        [JsonPropertyName("publishers")]
        public string[]? Publishers { get; set; }

        /// <summary>
        /// developers of the game
        /// </summary>
        /// <value>developers of the game</value>
        
        [JsonPropertyName("developers")]
        public string[]? Developers { get; set; }

        /// <summary>
        /// tags of the game
        /// </summary>
        /// <value>tags of the game</value>
        
        [JsonPropertyName("tags")]
        public string[]? Tags { get; set; }

        /// <summary>
        /// genres of the game
        /// </summary>
        /// <value>genres of the game</value>
        
        [JsonPropertyName("genres")]
        public string[]? Genres { get; set; }


    }
}
