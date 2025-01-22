using Newtonsoft.Json;

namespace CosmosDbAppService.Models
{
    public class ParticleEvent
    {
        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("EventId")]
        public string EventId { get; set; }

        [JsonProperty("Event")]
        public string Event { get; set; }

        [JsonProperty("Data")]
        public Dictionary<string, object> Data { get; set; }

        [JsonProperty("deviceId")]
        public string DeviceId { get; set; }

        [JsonProperty("PublishedAt")]
        public DateTime PublishedAt { get; set; }

        [JsonProperty("userid")]
        public string UserId { get; set; }

        [JsonProperty("ProductId")]
        public string ProductId { get; set; }

        [JsonProperty("FwVersion")]
        public string FwVersion { get; set; }
    }
}
