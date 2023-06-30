using ImageMagick;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace crackpipe.Models
{
    public class PaginatedData<T>
    {
        [JsonPropertyName("data")]
        public T[] Data { get; set; }
        [JsonPropertyName("meta")]
        public MetaData Meta { get; set; }
        [JsonPropertyName("links")]
        public Links Links { get; set; }
    }
    public class MetaData
    {
        //[JsonPropertyName("itemsPerPage")]
        //public long ItemsPerPage { get; set; }
        [JsonPropertyName("totalItems")]
        public int TotalItems { get; set; }
        //[JsonPropertyName("currentPage")]
        //public int CurrentPage { get; set; }
        //[JsonPropertyName("totalPages")]
        //public int TotalPages { get; set; }
        //[JsonPropertyName("sortBy")]
        //public string SortBy { get; set; }
        //[JsonPropertyName("search")]
        //public string Search { get; set; }
        //[JsonPropertyName("filter")]
        //public string[] Filter { get; set; }
    }
    public class Links
    {
        [JsonPropertyName("first")]
        public string First { get; set; }
        [JsonPropertyName("previous")]
        public string Previous { get; set; }
        [JsonPropertyName("current")]
        public string Current { get; set; }
        [JsonPropertyName("next")]
        public string Next { get; set; }
        [JsonPropertyName("last")]
        public string Last { get; set; }
    }
}
