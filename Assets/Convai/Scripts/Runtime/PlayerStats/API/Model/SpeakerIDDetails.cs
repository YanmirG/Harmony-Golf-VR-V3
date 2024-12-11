using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Convai.Scripts.Runtime.PlayerStats.API.Model {
    public class SpeakerIDDetails {
        [JsonProperty("speaker_id")] public string ID { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }
}
