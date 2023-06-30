using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace crackpipe.Models
{
    public struct Genre_Tag
    {
        [JsonPropertyName("id")]
        public int? ID { get; set; }
        [JsonPropertyName("rawg_id")]
        public int? RawgId { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
