using Newtonsoft.Json;

namespace SimpsonsAPI.Model
{
    public class CastMember
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        public string Name { get; set; }
        public string WikiUrl { get; set; }
        public string[] Characters { get; set; }
    }
}