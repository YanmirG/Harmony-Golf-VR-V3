using Convai.Scripts.Runtime.LoggerSystem;
using UnityEditor;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup.AccountsSection
{
    public class APIKeySetupUI
    {
        private readonly TextField _apiKeyField;
        private readonly Button _saveApiKeyButton;
        private readonly Button _togglePasswordButton;

        public APIKeySetupUI(VisualElement root)
        {
            _apiKeyField = root.Q<TextField>("api-key");
            _togglePasswordButton = root.Q<Button>("toggle-password");
            _saveApiKeyButton = root.Q<Button>("save-api-key");

            if (_apiKeyField == null || _togglePasswordButton == null || _saveApiKeyButton == null)
            {
                ConvaiLogger.Error("One or more UI elements not found. Check your UI structure.", ConvaiLogger.LogCategory.UI);
                return;
            }

            APIKeySetupLogic.LoadExistingApiKey(_apiKeyField, _saveApiKeyButton);
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            _togglePasswordButton.clicked += TogglePasswordVisibility;
            _saveApiKeyButton.clicked += () => ClickEvent(_apiKeyField.value);
        }

        private void TogglePasswordVisibility()
        {
            _apiKeyField.isPasswordField = !_apiKeyField.isPasswordField;
            _togglePasswordButton.text = _apiKeyField.isPasswordField ? "Show" : "Hide";
        }

        private async void ClickEvent(string apiKey)
        {
            (bool isSuccessful, bool shouldShowPage2) result = await APIKeySetupLogic.BeginButtonTask(apiKey);

            if (result.isSuccessful)
            {
                if (result.shouldShowPage2)
                    APIKeyReferralWindow.ShowWindow();
                else
                    EditorUtility.DisplayDialog("Success", "API Key loaded successfully!", "OK");

                _apiKeyField.isReadOnly = false;
                _saveApiKeyButton.text = "Update API Key";
                ConvaiSDKSetupEditorWindow.IsApiKeySet = true;
            }

            AccountInformationUI.GetUserAPIUsageData(result.isSuccessful);
        }
    }
}