using System;
using Convai.Scripts.Runtime.Attributes;
using Newtonsoft.Json;
using UnityEngine;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    [Serializable]
    public class UsageData
    {
        [JsonProperty("plan_name")] [ReadOnly] [SerializeField]
        public string planName;

        [JsonProperty("expiry_ts")] [ReadOnly] [SerializeField]
        public string expiryTs;

        [JsonProperty("daily_limit")] [ReadOnly] [SerializeField]
        public int dailyLimit;

        [JsonProperty("monthly_limit")] [ReadOnly] [SerializeField]
        public int monthlyLimit;

        [JsonProperty("extended_isAllowed")] [ReadOnly] [SerializeField]
        public bool extendedIsAllowed;

        [JsonProperty("daily_usage")] [ReadOnly] [SerializeField]
        public int dailyUsage;

        [JsonProperty("monthly_usage")] [ReadOnly] [SerializeField]
        public int monthlyUsage;

        [JsonProperty("extended_usage")] [ReadOnly] [SerializeField]
        public int extendedUsage;
    }
}