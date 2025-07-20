using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public enum PERMISSION_ROLE
    {
        GUEST,
        USER,
        EDITOR,
        ADMIN
    }
    public class User
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("avatar")]
        public Media Avatar { get; set; }
        [JsonPropertyName("background")]
        public Media Background { get; set; }
        [JsonPropertyName("email")]
        public string EMail { get; set; }
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string LastName { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
        [JsonPropertyName("api_key")]
        public string ApiKey { get; set; }
        [JsonPropertyName("progresses")]
        public Progress[]? Progresses { get; set; }       
        [JsonPropertyName("role")]
        public PERMISSION_ROLE? Role { get; set; }
        [JsonPropertyName("activated")]
        public bool? Activated { get; set; }
        [JsonPropertyName("deleted_at")]
        public string DeletedAt { get; set; }
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("birth_date")]
        public DateTime? BirthDate { get; set; }
    }
}
