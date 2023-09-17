using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class RawgGame
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("rawg_id")]
        public int? ID { get; set; }
        [JsonPropertyName("box_image_url")]
        public string BoxImageUrl { get; set; }
    }
}
