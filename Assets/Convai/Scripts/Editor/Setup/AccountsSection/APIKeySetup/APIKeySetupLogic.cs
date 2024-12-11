using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Runtime.LoggerSystem;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.VSAttribution;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    public abstract class APIKeySetupLogic
    {
        private const string API_KEY_ASSET_PATH = "Assets/Resources/ConvaiAPIKey.asset";
        private const string API_URL = "https://api.convai.com/user/referral-source-status";

        public static async Task<(bool isSuccessful, bool shouldShowPage2)> BeginButtonTask(string apiKey)
        {
            ConvaiAPIKeySetup aPIKeySetup = ScriptableObject.CreateInstance<ConvaiAPIKeySetup>();
            aPIKeySetup.APIKey = apiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a valid API Key.", "OK");
                return (false, false);
            }

            string referralStatus = await CheckReferralStatus(API_URL, apiKey);

            if (referralStatus == null)
            {
                EditorUtility.DisplayDialog("Error", "Something went wrong. Please check your API Key. Contact support@convai.com for more help.", "OK");
                return (false, false);
            }

            CreateOrUpdateAPIKeyAsset(aPIKeySetup);

            if (referralStatus.Trim().ToLower() != "undefined" && referralStatus.Trim().ToLower() != "") return (true, false);
            EditorUtility.DisplayDialog("Success", "[Step 1/2] API Key loaded successfully!", "OK");
            return (true, false);
        }

        private static async Task<string> CheckReferralStatus(string url, string apiKey)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "post";
            request.ContentType = "application/json";

            string bodyJsonString = "{}";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);
            request.Headers.Add("CONVAI-API-KEY", apiKey);

            await using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            try
            {
                using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                ReferralSourceStatus referralStatus = null;
                await using (Stream streamResponse = response.GetResponseStream())
                {
                    if (streamResponse != null)
                    {
                        using StreamReader reader = new(streamResponse);
                        string responseContent = await reader.ReadToEndAsync();
                        referralStatus = JsonConvert.DeserializeObject<ReferralSourceStatus>(responseContent);
                    }
                }

                return referralStatus?.ReferralSourceStatusProperty;
            }
            catch (WebException e)
            {
                ConvaiLogger.Error(e.Message + "\nPlease check if API Key is correct.", ConvaiLogger.LogCategory.GRPC);
                return null;
            }
            catch (Exception e)
            {
                ConvaiLogger.Error(e.Message, ConvaiLogger.LogCategory.GRPC);
                return null;
            }
        }

        public static async Task ContinueEvent(string selectedOption, string otherOption, APIKeyReferralWindow window)
        {
            List<string> attributionSourceOptions = new()
            {
                "Search Engine (Google, Bing, etc.)",
                "Youtube",
                "Social Media (Facebook, Instagram, TikTok, etc.)",
                "Friend Referral",
                "Unity Asset Store",
                "Others"
            };

            int currentChoiceIndex = attributionSourceOptions.IndexOf(selectedOption);

            if (currentChoiceIndex < 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid referral source!", "OK");
                return;
            }

            UpdateSource updateSource = new(attributionSourceOptions[currentChoiceIndex]);

            if (attributionSourceOptions[currentChoiceIndex] == "Others")
                updateSource.ReferralSource = otherOption;

            ConvaiAPIKeySetup apiKeyObject = AssetDatabase.LoadAssetAtPath<ConvaiAPIKeySetup>(API_KEY_ASSET_PATH);
            await SendReferralRequest("https://api.convai.com/user/update-source", JsonConvert.SerializeObject(updateSource), apiKeyObject.APIKey);

            if (attributionSourceOptions[currentChoiceIndex] == "Unity Asset Store")
                VSAttribution.SendAttributionEvent("Initial Setup", "Convai Technologies, Inc.", apiKeyObject.APIKey);

            EditorUtility.DisplayDialog("Success", "Setup completed successfully!", "OK");
            window.Close();
        }

        private static async Task SendReferralRequest(string url, string bodyJsonString, string apiKey)
        {
            WebRequest request = WebRequest.Create(url);
            request.Method = "post";
            request.ContentType = "application/json";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);
            request.Headers.Add("CONVAI-API-KEY", apiKey);

            await using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            try
            {
                using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                await using (Stream streamResponse = response.GetResponseStream())
                {
                    if (streamResponse != null)
                    {
                        using StreamReader reader = new(streamResponse);
                        await reader.ReadToEndAsync();
                    }
                }

                if ((int)response.StatusCode == 200) ConvaiLogger.DebugLog("Referral sent successfully.", ConvaiLogger.LogCategory.GRPC);
            }
            catch (WebException e)
            {
                ConvaiLogger.Error(e.Message + "\nPlease check if API Key is correct.", ConvaiLogger.LogCategory.GRPC);
            }
            catch (Exception e)
            {
                ConvaiLogger.Exception(e, ConvaiLogger.LogCategory.GRPC);
            }
        }

        private static void CreateOrUpdateAPIKeyAsset(ConvaiAPIKeySetup aPIKeySetup)
        {
            if (!File.Exists(API_KEY_ASSET_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                AssetDatabase.CreateAsset(aPIKeySetup, API_KEY_ASSET_PATH);
            }
            else
            {
                AssetDatabase.DeleteAsset(API_KEY_ASSET_PATH);
                AssetDatabase.CreateAsset(aPIKeySetup, API_KEY_ASSET_PATH);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void LoadExistingApiKey(TextField apiKeyField, Button saveApiKeyButton)
        {
            ConvaiAPIKeySetup existingApiKey = AssetDatabase.LoadAssetAtPath<ConvaiAPIKeySetup>(API_KEY_ASSET_PATH);
            if (existingApiKey != null && !string.IsNullOrEmpty(existingApiKey.APIKey))
            {
                apiKeyField.value = existingApiKey.APIKey;
                saveApiKeyButton.text = "Update API Key";
                ConvaiSDKSetupEditorWindow.IsApiKeySet = true;
            }
        }

        #region Nested type: ReferralSourceStatus

        public class ReferralSourceStatus
        {
            [JsonProperty("referral_source_status")]
            public string ReferralSourceStatusProperty;

            [JsonProperty("status")] public string Status;
        }

        #endregion

        #region Nested type: UpdateSource

        public class UpdateSource
        {
            [JsonProperty("referral_source")] public string ReferralSource;

            public UpdateSource(string referralSource)
            {
                ReferralSource = referralSource;
            }
        }

        #endregion
    }
}