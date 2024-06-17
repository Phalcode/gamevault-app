using System;
using System.ComponentModel;
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
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("rawg_title")]
        public string RawgTitle { get; set; }
        [JsonPropertyName("box_image")]
        public Image BoxImage { get; set; }
        [JsonPropertyName("rawg_id")]
        public int? RawgId { get; set; }

        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("background_image")]
        public Image BackgroundImage { get; set; }
        [JsonPropertyName("release_date")]
        public DateTime? ReleaseDate { get; set; }
        [JsonPropertyName("rawg_release_date")]
        public DateTime? RawgReleaseDate { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("cache_date")]
        public DateTime? LastCached { get; set; }
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("website_url")]
        public string WebsiteUrl { get; set; }
        [JsonPropertyName("type")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public GameType Type { get; set; }
        [JsonPropertyName("metacritic_rating")]
        public int? Rating { get; set; }
        [JsonPropertyName("average_playtime")]
        public int? AveragePlaytime { get; set; }
        [JsonPropertyName("deleted_at")]
        public DateTime? Deleted_At { get; set; }
        [JsonPropertyName("early_access")]
        public bool EarlyAccess { get; set; }
        [JsonPropertyName("developers")]
        public Developer[] Developers { get; set; }
        [JsonPropertyName("publishers")]
        public Publisher[] Publishers { get; set; }
        
        [JsonPropertyName("tags")]
        public Tag[] Tags { get; set; }
        [JsonPropertyName("genres")]
        public Genre[] Genres { get; set; }
        [JsonPropertyName("progresses")]
        public Progress[] Progresses { get; set; }
        [JsonPropertyName("bookmarked_users")]
        public User[]? BookmarkedUsers { get; set; }
    }
}
