using System;
using System.Text.Json.Serialization;

namespace crackpipe.Models
{
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
        public DateTime ReleaseDate { get; set; }
        [JsonPropertyName("file_path")]
        public string FilePath { get; set; }
        [JsonPropertyName("size")]
        public string Size { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("website_url")]
        public string WebsiteUrl { get; set; }
        [JsonPropertyName("metacritic_rating")]
        public int? Rating { get; set; }
        [JsonPropertyName("average_playtime")]
        public int? AveragePlaytime { get; set; }
        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }
        [JsonPropertyName("early_access")]
        public bool EarlyAccess { get; set; }
        [JsonPropertyName("developers")]
        public Developer[] Developers { get; set; }
        [JsonPropertyName("publishers")]
        public Publisher[] Publishers { get; set; }
        [JsonPropertyName("stores")]
        public Store[] Stores { get; set; }
        [JsonPropertyName("tags")]
        public Tag[] Tags { get; set; }
        [JsonPropertyName("genres")]
        public Genre[] Genres { get; set; }
    }
}
