using System;
using Convai.Scripts.Runtime.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     Data class for Section Data
    /// </summary>
    [Serializable]
    public class SectionData
    {
        [JsonProperty("section_id")] [ReadOnly] [SerializeField]
        public string sectionId;

        [JsonProperty("section_name")] [ReadOnly] [SerializeField]
        public string sectionName;

        [JsonProperty("bt_constants")] [HideInInspector] [SerializeField]
        public string behaviorTreeConstants;

        [JsonProperty("objective")] [ReadOnly] [SerializeField]
        public string objective;

        [JsonProperty("character_id")] [ReadOnly] [HideInInspector] [SerializeField]
        public string characterId;

        [JsonProperty("decisions")] [ReadOnly] public object Decisions;

        [JsonProperty("parents")] [ReadOnly] public object Parents;

        [JsonProperty("triggers")] [ReadOnly] public object Triggers;

        [JsonProperty("updated_character_data")] [ReadOnly]
        public object UpdatedCharacterData;
    }
}