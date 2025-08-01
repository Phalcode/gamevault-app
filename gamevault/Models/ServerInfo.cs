using gamevault.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;

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
    public class BindableServerInfo
    {
        public bool IsAvailable { get; set; }

        public bool IsRegistrationEnabled { get; set; }
        public bool IsFirstNameMandatory { get; set; }
        public bool IsLastNameMandatory { get; set; }
        public bool IsEMailMandatory { get; set; }
        public bool IsBirthDateMandatory { get; set; }

        public bool IsBasicAuthEnabled { get; set; }
        public bool IsSSOEnabled { get; set; }
        public string ErrorMessage { get; set; }
        public bool HasError { get; set; }

        public BindableServerInfo(string errorMessage = "")
        {
            if (errorMessage != "")
            {
                HasError = true;
                ErrorMessage = errorMessage;
            }
        }
        public BindableServerInfo(ServerInfo info)
        {
            IsAvailable = true;
            IsRegistrationEnabled = info.RegistrationEnabled;
            IsFirstNameMandatory = info.RequiredRegistrationFields.Contains("first_name");
            IsLastNameMandatory = info.RequiredRegistrationFields.Contains("last_name");
            IsEMailMandatory = info.RequiredRegistrationFields.Contains("email");
            IsBirthDateMandatory = info.RequiredRegistrationFields.Contains("birth_date");
            IsBasicAuthEnabled = info.AvailableAuthenticationMethods.Contains("basic");
            IsSSOEnabled = info.AvailableAuthenticationMethods.Contains("sso");
        }
    }
}
