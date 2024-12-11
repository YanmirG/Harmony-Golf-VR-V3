using System;
using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.PlayerStats.API.Model
{
    [Serializable]
    public class CharacterUpdateRequest
    {
        public CharacterUpdateRequest(string characterID, bool isEnabled)
        {
            CharacterID = characterID;
            MemorySettings = new MemorySettings(isEnabled);
        }

        [JsonProperty("charID")] public string CharacterID { get; set; }
        [JsonProperty("memorySettings")] public MemorySettings MemorySettings { get; set; }
    }
}