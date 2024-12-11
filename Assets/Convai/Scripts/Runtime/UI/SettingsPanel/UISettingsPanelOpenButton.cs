using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    public class UISettingsPanelOpenButton : MonoBehaviour
    {
        private Button _openButton;
        private UISettingsPanel _uiSettingsPanel;

        private void Awake()
        {
            _uiSettingsPanel = FindObjectOfType<UISettingsPanel>();
            _openButton = GetComponent<Button>();

            _openButton.onClick.AddListener(OpenSettingsPanel);
        }

        private void OpenSettingsPanel()
        {
            _uiSettingsPanel.ToggleSettingsPanel(true);
        }
    }
}