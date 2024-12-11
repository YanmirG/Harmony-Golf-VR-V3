using System.Collections;
using Convai.Scripts.Runtime.PlayerStats;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     This class is used to manage the display name settings in the UI.
    /// </summary>
    public class UIDisplayNameSettings : MonoBehaviour
    {
        /// <summary>
        ///     Reference to the TextMeshPro input field for entering/displaying the display name.
        /// </summary>
        [SerializeField] private TMP_InputField playerNameInputField;

        private ConvaiPlayerDataSO _convaiPlayerData;
        private bool _hasPlayerNameBeenSaved;
        private string _originalPlayerName;
        private UISettingsPanel _uiSettingsPanel;

        private void Awake()
        {
            _originalPlayerName = string.Empty;
            _uiSettingsPanel = GetComponentInParent<UISettingsPanel>();

            _uiSettingsPanel.SaveChangesButton.onClick.AddListener(() =>
            {
                _hasPlayerNameBeenSaved = true;
                _originalPlayerName = string.Empty;
            });

            _uiSettingsPanel.SettingsPanelExitButton.onClick.AddListener(() =>
            {
                if (_hasPlayerNameBeenSaved || string.IsNullOrEmpty(_originalPlayerName)) return;
                playerNameInputField.text = _originalPlayerName;
                _originalPlayerName = string.Empty;
            });
        }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f);
            if (ConvaiPlayerDataSO.GetPlayerData(out _convaiPlayerData))
                playerNameInputField.text = string.IsNullOrEmpty(_convaiPlayerData.PlayerName) ? _convaiPlayerData.DefaultPlayerName : _convaiPlayerData.PlayerName;
        }

        private void OnEnable()
        {
            UISaveLoadSystem.Instance.OnSave += UISaveLoadSystem_OnSave;
            playerNameInputField.onSelect.AddListener(PlayerNameInputField_OnSelect);
        }

        private void OnDisable()
        {
            UISaveLoadSystem.Instance.OnSave -= UISaveLoadSystem_OnSave;
            playerNameInputField.onSelect.RemoveListener(PlayerNameInputField_OnSelect);
        }

        private void PlayerNameInputField_OnSelect(string value)
        {
            if (!string.IsNullOrEmpty(_originalPlayerName)) return;
            _originalPlayerName = value;
        }

        private void UISaveLoadSystem_OnSave()
        {
            if (_convaiPlayerData == null || !_hasPlayerNameBeenSaved || string.IsNullOrEmpty(playerNameInputField.text)) return;
            _convaiPlayerData.PlayerName = playerNameInputField.text;
        }
    }
}