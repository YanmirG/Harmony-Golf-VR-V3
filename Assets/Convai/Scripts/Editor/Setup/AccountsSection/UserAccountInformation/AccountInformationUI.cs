using System;
using System.Globalization;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    public class AccountInformationUI
    {
        private static Label _planNameLabel;
        private static Label _expiryTsLabel;
        private static ProgressBar _dailyUsageBar;
        private static ProgressBar _monthlyUsageBar;
        private static Label _dailyUsageLabel;
        private static Label _monthlyUsageLabel;

        public AccountInformationUI(VisualElement root)
        {
            _planNameLabel = root.Q<Label>("plan-name");
            _expiryTsLabel = root.Q<Label>("expiry-date");
            _dailyUsageBar = root.Q<ProgressBar>("DailyUsageBar");
            _monthlyUsageBar = root.Q<ProgressBar>("MonthlyUsageBar");
            _dailyUsageLabel = root.Q<Label>("daily-usage-label");
            _monthlyUsageLabel = root.Q<Label>("monthly-usage-label");

            SetupApiKeyField(root);
        }

        private static void SetupApiKeyField(VisualElement root)
        {
            TextField apiKeyField = root.Q<TextField>("api-key");
            if (apiKeyField != null && ConvaiAPIKeySetup.GetAPIKey(out string apiKey))
            {
                apiKeyField.value = apiKey;
                apiKeyField.isReadOnly = false;
                ConvaiSDKSetupEditorWindow.IsApiKeySet = true;
                // apiKeyField.RegisterValueChangedCallback(evt =>
                // {
                //     if (!string.IsNullOrEmpty(evt.newValue))
                //     {
                //         apiKeyField.value = string.Empty;
                //     }
                // });
                // _ = new UserAPIUsage();
                GetUserAPIUsageData();
            }
            else
            {
                ConvaiSDKSetupEditorWindow.IsApiKeySet = false;
            }
        }

        public static async void GetUserAPIUsageData(bool validApiKey = true)
        {
            if (!validApiKey)
            {
                SetInvalidApiKeyUI();
                return;
            }

            try
            {
                string userAPIUsage = await UserAPIUsage.GetUserAPIUsage();
                JObject jsonObject = JObject.Parse(userAPIUsage);
                UsageData usageData = jsonObject["usage"]?.ToObject<UsageData>();

                if (usageData != null)
                {
                    UpdateUIWithUsageData(usageData);
                }
                else
                {
                    ConvaiLogger.Warn("Failed to parse usage data.", ConvaiLogger.LogCategory.GRPC);
                    SetInvalidApiKeyUI();
                }
            }
            catch (Exception ex)
            {
                ConvaiLogger.Exception($"Error fetching API usage data: {ex.Message}", ConvaiLogger.LogCategory.GRPC);
                SetInvalidApiKeyUI();
            }
        }

        private static void UpdateUIWithUsageData(UsageData usageData)
        {
            _planNameLabel.text = usageData.planName;
            _expiryTsLabel.text = GetFormattedDate(usageData.expiryTs);
            _dailyUsageBar.value = CalculateUsagePercentage(usageData.dailyUsage, usageData.dailyLimit);
            _monthlyUsageBar.value = CalculateUsagePercentage(usageData.monthlyUsage, usageData.monthlyLimit);
            _dailyUsageLabel.text = FormatUsageLabel(usageData.dailyUsage, usageData.dailyLimit);
            _monthlyUsageLabel.text = FormatUsageLabel(usageData.monthlyUsage, usageData.monthlyLimit);
        }

        private static void SetInvalidApiKeyUI()
        {
            _planNameLabel.text = "Invalid API Key";
            _expiryTsLabel.text = "Invalid API Key";
            _dailyUsageBar.value = 0;
            _monthlyUsageBar.value = 0;
            _dailyUsageLabel.text = "0/0 interactions";
            _monthlyUsageLabel.text = "0/0 interactions";
        }

        private static float CalculateUsagePercentage(int usage, int limit)
        {
            return limit > 0 ? (float)usage / limit * 100 : 0;
        }

        private static string FormatUsageLabel(int usage, int limit)
        {
            return $"{usage}/{limit} interactions";
        }

        private static string GetFormattedDate(string dateString)
        {
            if (string.IsNullOrEmpty(dateString)) return dateString;

            if (DateTime.TryParseExact(dateString, "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                string daySuffix = GetDaySuffix(date.Day);
                return date.ToString($"MMMM dd'{daySuffix}' yyyy");
            }

            ConvaiLogger.Warn($"Failed to parse date: {dateString}", ConvaiLogger.LogCategory.GRPC);
            return dateString;
        }

        private static string GetDaySuffix(int day)
        {
            return (day % 10, day / 10) switch
            {
                (1, 1) or (2, 1) or (3, 1) => "th",
                (1, _) => "st",
                (2, _) => "nd",
                (3, _) => "rd",
                _ => "th"
            };
        }
    }
}