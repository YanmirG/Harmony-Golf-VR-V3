using System;
using Newtonsoft.Json;

namespace Convai.Scripts.Runtime.PlayerStats.API.Model
{
    [Serializable]
    public class MemorySettings
    {
        public MemorySettings(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }

        [JsonProperty("enabled")] public bool IsEnabled { get; set; }
    }
}