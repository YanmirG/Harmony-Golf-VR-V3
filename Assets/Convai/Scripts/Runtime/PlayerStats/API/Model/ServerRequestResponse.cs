using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.PlayerStats.API.Model
{
    public class ServerRequestResponse
    {
        [JsonProperty("STATUS")] public string Status { get; private set; }

        [JsonProperty("speaker_id")] public string SpeakerID { get; private set; }
    }
}