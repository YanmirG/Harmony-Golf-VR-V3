using Convai.Scripts.Runtime.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     The UISettingsPanel class manages the settings panel UI, including animations and user interactions.
    /// </summary>
    public class UISettingsPanel : MonoBehaviour
    {
        [field: Header("References")]
        [field: SerializeField]
        public Button SaveChangesButton { get; private set; }

        [field: SerializeField] public Button SettingsPanelExitButton { get; private set; }
        [SerializeField] private CanvasGroup _panelCanvasGroup;

        /// <summary>
        ///     Animation durations for fade in, fade out, and gap between animations
        /// </summary>
        [Header("Settings Panel Animation Values")] [SerializeField] [Tooltip("Duration for fade in animation")]
        private float _fadeInDuration = 0.35f;

        [SerializeField] [Tooltip("Duration for fade out animation")]
        private float _fadeOutDuration = 0.35f;

        private FadeCanvas _fadeCanvas;

        /// <summary>
        ///     References to other scripts and components
        /// </summary>
        private UIAppearanceSettings _uiAppearanceSettings;

        /// <summary>
        ///     Initialize references to other scripts and components and populate the dropdown with available microphone devices.
        /// </summary>
        private void Awake()
        {
            // Initialize references to other scripts and components
            _fadeCanvas = GetComponent<FadeCanvas>();
            _uiAppearanceSettings = GetComponent<UIAppearanceSettings>();

            // Attach event listeners to UI buttons
            SaveChangesButton.onClick.AddListener(SaveChanges);
            SettingsPanelExitButton.onClick.AddListener(delegate { ToggleSettingsPanel(false); });
        }

        private void OnEnable()
        {
            ConvaiInputManager.Instance.toggleSettings += ToggleSettings;
        }

        private void OnDisable()
        {
            ConvaiInputManager.Instance.toggleSettings -= ToggleSettings;
        }

        /// <summary>
        ///     Update is called once per frame
        /// </summary>
        private void ToggleSettings()
        {
            if (_panelCanvasGroup.alpha == 0)
            {
                ToggleSettingsPanel(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                ToggleSettingsPanel(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        /// <summary>
        ///     Toggle the settings panel on or off
        /// </summary>
        public void ToggleSettingsPanel(bool value)
        {
            if (value)
            {
                // Fade in the settings panel
                ActivatePanel();
                FadeInSettingsPanel();

                // Check if the alpha of the appearance canvas is at its maximum
                const float MAX_ALPHA = 1;
                if (_uiAppearanceSettings.GetCurrentActiveAppearance().GetCanvasGroup().alpha >= MAX_ALPHA)
                    // If true, fade out the current appearance canvas
                    _uiAppearanceSettings.FadeOutCurrentAppearance();

                // Set the cursor lock state to none
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                // Fade out the settings panel
                _fadeCanvas.OnCurrentFadeCompleted += DeactivatePanel;
                FadeOutSettingsPanel();

                // Check if there is an active Convai NPC
                if (ConvaiNPCManager.Instance.GetActiveConvaiNPC() != null)
                    // If true, fade in the current appearance canvas
                    _uiAppearanceSettings.FadeInCurrentAppearance();

                // Set the cursor lock state to locked
                Cursor.lockState = CursorLockMode.Locked;

                // Save values when the settings panel is closed
                UISaveLoadSystem.Instance.SaveValues();
            }
        }

        /// <summary>
        ///     Save changes and close the settings panel
        /// </summary>
        private void SaveChanges()
        {
            UISaveLoadSystem.Instance.SaveValues();
            ToggleSettingsPanel(false);
        }

        /// <summary>
        ///     Trigger fade in animation for the settings panel
        /// </summary>
        private void FadeInSettingsPanel()
        {
            _fadeCanvas.StartFadeIn(_panelCanvasGroup, _fadeInDuration);
        }

        /// <summary>
        ///     Trigger fade out animation for the settings panel
        /// </summary>
        private void FadeOutSettingsPanel()
        {
            _fadeCanvas.StartFadeOut(_panelCanvasGroup, _fadeOutDuration);
        }

        /// <summary>
        ///     Trigger fade out and fade in animation with a gap in between
        /// </summary>
        public void FadeOutFadeinWithGap(float fadeInDuration, float fadeOutDuration, float previewDuration)
        {
            _fadeCanvas.StartFadeOutFadeInWithGap(_panelCanvasGroup, fadeInDuration, fadeOutDuration, previewDuration);
        }

        private void DeactivatePanel()
        {
            _panelCanvasGroup.gameObject.SetActive(false);
            _fadeCanvas.OnCurrentFadeCompleted -= DeactivatePanel;
        }

        private void ActivatePanel()
        {
            _panelCanvasGroup.gameObject.SetActive(true);
        }
    }
}