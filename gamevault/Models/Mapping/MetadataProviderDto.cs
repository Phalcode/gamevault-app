using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models.Mapping
{
    public class MetadataProviderDto
    {      
        [JsonPropertyName("slug")]
        public string? Slug { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }
        [JsonPropertyName("enabled")]
        public bool? Enabled { get; set; }       
    }
}
