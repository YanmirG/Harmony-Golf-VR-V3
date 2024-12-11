using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.Addons
{
    /// <summary>
    ///     Monitors the duration of the talk button press and notifies the Notification System if released prematurely.
    /// </summary>
    public class TalkButtonDurationChecker : MonoBehaviour
    {
        /// <summary>
        ///     Minimum duration required for a valid talk action.
        /// </summary>
        private const float MIN_TALK_DURATION = 0.5f;

        /// <summary>
        ///     Flag indicating whether the talk button was released prematurely.
        /// </summary>
        [HideInInspector] public bool isTalkKeyReleasedEarly;

        private TMP_InputField _activeInputField;

        /// <summary>
        ///     Timer to track the duration of the talk button press.
        /// </summary>
        private float _timer;

        private UIAppearanceSettings _uiAppearanceSettings;

        private void Awake()
        {
            _uiAppearanceSettings = FindObjectOfType<UIAppearanceSettings>();
        }

        /// <summary>
        ///     Update is called once per frame.
        ///     It checks if the talk button is being held down or released.
        /// </summary>
        private void Update()
        {
            // Check if the talk button is being held down and increment the timer based on the time passed since the last frame.
            if (ConvaiInputManager.Instance.IsTalkKeyHeld && !UIUtilities.IsAnyInputFieldFocused()) _timer += Time.deltaTime;
        }

        private void OnEnable()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged += ConvaiNPCManager_OnActiveNPCChanged;
            _uiAppearanceSettings.OnAppearanceChanged += UIAppearanceSettings_OnAppearanceChanged;
            ConvaiInputManager.Instance.talkKeyInteract += HandleTalkButtonRelease;
        }

        private void OnDisable()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged -= ConvaiNPCManager_OnActiveNPCChanged;
            _uiAppearanceSettings.OnAppearanceChanged -= UIAppearanceSettings_OnAppearanceChanged;
        }

        private void HandleTalkButtonRelease(bool releaseState)
        {
            if (releaseState || UIUtilities.IsAnyInputFieldFocused()) return;

            if (_activeInputField != null && _activeInputField.isFocused)
            {
                _timer = 0;
                return;
            }

            CheckTalkButtonRelease();
            // Reset the timer for the next talk action.
            _timer = 0;
        }

        private void ConvaiNPCManager_OnActiveNPCChanged(ConvaiNPC convaiNpc)
        {
            if (convaiNpc == null)
            {
                _activeInputField = null;
                return;
            }

            _activeInputField = convaiNpc.playerInteractionManager.FindActiveInputField();
        }

        private void UIAppearanceSettings_OnAppearanceChanged()
        {
            ConvaiNPC convaiNpc = ConvaiNPCManager.Instance.activeConvaiNPC;
            if (convaiNpc == null)
            {
                _activeInputField = null;
                return;
            }

            _activeInputField = convaiNpc.playerInteractionManager.FindActiveInputField();
        }

        /// <summary>
        ///     Checks if the talk button was released prematurely and triggers a notification if so.
        /// </summary>
        private void CheckTalkButtonRelease()
        {
            // Initialize the flag to false.
            isTalkKeyReleasedEarly = false;

            // Trigger a notification if the talk button is released before reaching the minimum required duration.
            if (_timer < MIN_TALK_DURATION)
            {
                // Check if there is an active ConvaiNPC.
                if (ConvaiNPCManager.Instance.activeConvaiNPC == null) return;

                // Set the flag to true and request a notification.
                isTalkKeyReleasedEarly = true;
                NotificationSystemHandler.Instance.NotificationRequest(NotificationType.TalkButtonReleasedEarly);
            }
        }
    }
}