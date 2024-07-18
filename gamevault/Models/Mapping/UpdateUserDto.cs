using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models.Mapping
{
    public class UpdateUserDto
    {
        /// <summary>
        /// username of the user
        /// </summary>
        /// <value>username of the user</value>
        [JsonPropertyName("username")]
        public string Username { get; set; }

        /// <summary>
        /// email of the user
        /// </summary>
        /// <value>email of the user</value>
        [JsonPropertyName("email")]
        public string EMail { get; set; }

        /// <summary>
        /// password of the user
        /// </summary>
        /// <value>password of the user</value>
        [JsonPropertyName("password")]
        public string Password { get; set; }

        /// <summary>
        /// first name of the user
        /// </summary>
        /// <value>first name of the user</value>
        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        /// <summary>
        /// last name of the user
        /// </summary>
        /// <value>last name of the user</value>
        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        /// <summary>
        /// date of birth of the user
        /// </summary>
        /// <value>date of birth of the user</value>
        [JsonPropertyName("birth_date")]
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// id of the avatar image of the user
        /// </summary>
        /// <value>id of the avatar image of the user</value>
        [JsonPropertyName("avatar_id")]
        public int? AvatarId { get; set; }

        /// <summary>
        /// id of the background image of the User
        /// </summary>
        /// <value>id of the background image of the User</value>
        [JsonPropertyName("background_id")]
        public int? BackgroundId { get; set; }

        /// <summary>
        /// wether or not the user is activated. Not yet working.
        /// </summary>
        /// <value>wether or not the user is activated. Not yet working.</value>
        [JsonPropertyName("activated")]
        public bool? Activated { get; set; }

        /// <summary>
        /// The role determines the set of permissions and access rights for a user in the system.
        /// </summary>
        /// <value>The role determines the set of permissions and access rights for a user in the system.</value>
        [JsonPropertyName("role")]
        public PERMISSION_ROLE? Role { get; set; }
    }
}
