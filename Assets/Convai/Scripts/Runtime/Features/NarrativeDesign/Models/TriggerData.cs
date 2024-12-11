using System;
using Convai.Scripts.Runtime.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    [Serializable]
    public class TriggerData
    {
        [JsonProperty("trigger_id")] [ReadOnly] [SerializeField]
        public string triggerId;

        [JsonProperty("trigger_name")] [ReadOnly] [SerializeField]
        public string triggerName;

        [JsonProperty("trigger_message")] [ReadOnly] [SerializeField]
        public string triggerMessage;

        [JsonProperty("destination_section")] [ReadOnly] [SerializeField]
        public string destinationSection;

        [JsonProperty("character_id")] [HideInInspector] [SerializeField]
        public string characterId;
    }
}