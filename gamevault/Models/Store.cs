using System.Text.Json.Serialization;

namespace gamevault.Models
{
    public struct Store
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("rawg_id")]
        public int RawgId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
