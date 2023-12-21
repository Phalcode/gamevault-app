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
        [Description("Unplayed")]
        UNPLAYED,
        [Description("Infinite")]
        INFINITE,
        [Description("Playing")]
        PLAYING,
        [Description("Completed")]
        COMPLETED,
        [Description("Aborted Temporary")]
        ABORTED_TEMPORARY,
        [Description("Aborted Permanent")]
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
