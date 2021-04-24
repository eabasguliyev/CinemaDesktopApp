using Newtonsoft.Json;

namespace CinemaWPF.Entities
{
    public struct ApiConfig
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}