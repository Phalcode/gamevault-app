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
    internal class User
    {
        [JsonPropertyName("id")]
        public int ID { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
        [JsonPropertyName("profile_picture")]
        public Image ProfilePicture { get; set; }
        [JsonPropertyName("background_image")]
        public Image BackgroundImage { get; set; }
        [JsonPropertyName("email")]
        public string EMail { get; set; }
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string LastName { get; set; }
        [JsonPropertyName("password")]
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
        [JsonPropertyName("progresses")]
        public Progress[]? Progresses { get; set; }
        [JsonPropertyName("profile_picture_url")]
        public string ProfilePictureUrl { get; set; }
        [JsonPropertyName("profile_picture_id")]
        public long? ProfilePictureId { get; set; }
        [JsonPropertyName("background_image_url")]
        public string BackgroundImageUrl { get; set; }
        [JsonPropertyName("background_image_id")]
        public long? BackgroundImageId { get; set; }
        [JsonPropertyName("role")]
        public PERMISSION_ROLE? Role { get; set; }
        [JsonPropertyName("activated")]
        public bool? Activated { get; set; }
        [JsonPropertyName("deleted_at")]
        public string DeletedAt { get; set; }
    }
}
