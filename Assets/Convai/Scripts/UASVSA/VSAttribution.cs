using System;
using UnityEngine.Analytics;

namespace UnityEditor.VSAttribution
{
    public static class VSAttribution
    {
        private const int k_VersionId = 4;
        private const int k_MaxEventsPerHour = 10;
        private const int k_MaxNumberOfElements = 1000;

        private const string k_VendorKey = "unity.vsp-attribution";
        private const string k_EventName = "vspAttribution";

        private static bool RegisterEvent()
        {
            AnalyticsResult result = EditorAnalytics.RegisterEventWithLimit(k_EventName, k_MaxEventsPerHour,
                k_MaxNumberOfElements, k_VendorKey, k_VersionId);

            bool isResultOk = result == AnalyticsResult.Ok;
            return isResultOk;
        }

        /// <summary>
        ///     Registers and attempts to send a Verified Solutions Attribution event.
        /// </summary>
        /// <param name="actionName">Name of the action, identifying a place this event was called from.</param>
        /// <param name="partnerName">Identifiable Verified Solutions Partner's name.</param>
        /// <param name="customerUid">Unique identifier of the customer using Partner's Verified Solution.</param>
        public static AnalyticsResult SendAttributionEvent(string actionName, string partnerName, string customerUid)
        {
            try
            {
                // Are Editor Analytics enabled ? (Preferences)
                if (!EditorAnalytics.enabled)
                    return AnalyticsResult.AnalyticsDisabled;

                if (!RegisterEvent())
                    return AnalyticsResult.InvalidData;

                // Create an expected data object
                VSAttributionData eventData = new()
                {
                    actionName = actionName,
                    partnerName = partnerName,
                    customerUid = customerUid,
                    extra = "{}"
                };

                return EditorAnalytics.SendEventWithLimit(k_EventName, eventData, k_VersionId);
            }
            catch
            {
                // Fail silently
                return AnalyticsResult.AnalyticsDisabled;
            }
        }

        #region Nested type: VSAttributionData

        [Serializable]
        private struct VSAttributionData
        {
            public string actionName;
            public string partnerName;
            public string customerUid;
            public string extra;
        }

        #endregion
    }
}