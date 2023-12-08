using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public enum State
    {
        UNPLAYED,
        INFINITE,
        PLAYING,
        COMPLETED,
        ABORTED_TEMPORARY,
        ABORTED_PERMANENT
    }
    public class Progress
    {
        [JsonPropertyName("id")]
        public int? ID { get; set; }
        [JsonPropertyName("minutes_played")]
        public int? MinutesPlayed { get; set; }
        [JsonPropertyName("state")]
        public string? State { get; set; }
        [JsonPropertyName("last_played_at")]
        public DateTime? LastPlayedAt { get; set; }
        [JsonPropertyName("game")]
        public Game? Game { get; set; }
        [JsonPropertyName("user")]
        public User? User { get; set; }

    }

}
