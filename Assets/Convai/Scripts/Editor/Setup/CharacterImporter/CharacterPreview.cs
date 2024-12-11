using System;
using System.Collections.Generic;

namespace Convai.Scripts.Editor.Setup.CharacterImporter
{
    [Serializable]
    public class CharacterPreview
    {
        public string character_name;
        public string user_id;
        public string character_id;
        public string listing;
        public List<string> language_codes;
        public string voice_type;
        public List<string> character_actions;
        public List<string> character_emotions;
        public ModelDetails model_details;
        public string language_code;
        public GuardrailMeta guardrail_meta;
        public CharacterTraits character_traits;
        public string timestamp;
        public int verbosity;
        public string organization_id;
        public bool is_narrative_driven;
        public string start_narrative_section_id;
        public bool moderation_enabled;
        public List<string> pronunciations;
        public List<string> boosted_words;
        public List<string> allowed_moderation_filters;
        public MemorySettings memory_settings;
        public string uncensored_access_consent;
        public string nsfw_model_size;
        public string temperature;
        public string backstory;
        public bool edit_character_access;
    }

    [Serializable]
    public class ModelDetails
    {
        public string modelType;
        public string modelLink;
        public string modelPlaceholder;
    }

    [Serializable]
    public class GuardrailMeta
    {
        public int limitResponseLevel;
        public List<string> blockedWords;
    }

    [Serializable]
    public class CharacterTraits
    {
        public List<string> catch_phrases;
        public string speaking_style;
        public PersonalityTraits personality_traits;
    }

    [Serializable]
    public class PersonalityTraits
    {
        public int openness;
        public int sensitivity;
        public int extraversion;
        public int agreeableness;
        public int meticulousness;
    }

    [Serializable]
    public class MemorySettings
    {
        public bool enabled;
    }
}