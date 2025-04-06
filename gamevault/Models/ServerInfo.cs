using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public struct ServerInfo
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }
        [JsonPropertyName("version")]
        public string Version { get; set; }
        [JsonPropertyName("registration_enabled")]
        public bool RegistrationEnabled { get; set; }
        [JsonPropertyName("required_registration_fields")]
        public string[] RequiredRegistrationFields { get; set; }
        [JsonPropertyName("available_authentication_methods")]
        public string[] AvailableAuthenticationMethods { get; set; }
    }
}
