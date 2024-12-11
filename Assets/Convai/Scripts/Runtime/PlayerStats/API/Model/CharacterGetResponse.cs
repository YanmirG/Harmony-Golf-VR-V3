using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.PlayerStats.API.Model
{
    public class CharacterGetResponse
    {
        [JsonProperty("memory_settings")] public MemorySettings MemorySettings { get; set; }
    }
}