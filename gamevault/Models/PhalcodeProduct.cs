using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace gamevault.Models
{
    public class PhalcodeProduct
    {
        [JsonPropertyName("stripe_id")]
        public string? StripeID { get; set; }
        [JsonPropertyName("stripe_subscription_item_id")]
        public string? StripeSubscriptionItemID { get; set; }
        [JsonPropertyName("price_stripe_id")]
        public string? PriceStripeID { get; set; }
        [JsonPropertyName("product_stripe_id")]
        public string? ProductStripeID { get; set; }
        [JsonPropertyName("customer_stripe_id")]
        public string? CustomerStripeID { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }
        [JsonPropertyName("current_period_start")]
        public DateTime? CurrentPeriodStart { get; set; }
        [JsonPropertyName("current_period_end")]
        public DateTime? CurrentPeriodEnd { get; set; }
        [JsonPropertyName("started_at")]
        public DateTime? StartedAt { get; set; }
        [JsonPropertyName("ended_at")]
        public DateTime? EndedAt { get; set; }
        [JsonPropertyName("cancel_at")]
        public DateTime? CancelAt { get; set; }
        [JsonPropertyName("trial_ends_at")]
        public DateTime? TrialEndsAt { get; set; }
        [JsonPropertyName("cancel_at_period_end")]
        public bool? CancelAtPeriodEnd { get; set; }
        public string? UserName { get; set; }
        public bool IsActive()
        {          
            return (CurrentPeriodEnd != null && CurrentPeriodEnd > DateTime.UtcNow);
        }
    }
}
