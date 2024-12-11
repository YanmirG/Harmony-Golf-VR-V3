using System;
using System.Collections.Generic;
using System.Linq;
using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Extensions;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     Base class for chat UI components, providing common functionality and abstract methods to be implemented by derived
    ///     classes.
    /// </summary>
    public abstract class ChatUIBase : MonoBehaviour, IChatUI
    {
        [SerializeField] protected GameObject recordingMarker;
        private readonly List<Character> _characters = new();
        private float _markerInitialAlpha;

        private Image _recordingMarkerImage;
        [NonSerialized] protected GameObject UIInstance;

        /// <summary>
        ///     Initializes the recording marker and subscribes to the OnPlayerSpeakingChanged event.
        /// </summary>
        protected virtual void Start()
        {
            SetupMarkerImage();
            SetRecordingMarkerActive(true);
            ConvaiGRPCAPI.Instance.OnPlayerSpeakingChanged += OnPlayerSpeakingChanged;
        }

        /// <summary>
        ///     Initializes the UI with the provided prefab.
        /// </summary>
        /// <param name="uiPrefab">The UI prefab to instantiate.</param>
        public abstract void Initialize(GameObject uiPrefab);

        /// <summary>
        ///     Activates the UI instance if transcript UI status is active.
        /// </summary>
        public virtual void ActivateUI()
        {
            if (UISaveLoadSystem.Instance.TranscriptUIActiveStatus)
            {
                SetUIActive(true);
                UIStatusChange?.Invoke(true);
            }
        }

        /// <summary>
        ///     Deactivates the UI instance.
        /// </summary>
        public virtual void DeactivateUI()
        {
            SetUIActive(false);
            UIStatusChange.Invoke(false);
        }

        /// <summary>
        ///     Sends character text to the UI.
        /// </summary>
        /// <param name="charName">The name of the character.</param>
        /// <param name="text">The text to send.</param>
        /// <param name="characterTextColor">The color of the character's text.</param>
        public abstract void SendCharacterText(string charName, string text, Color characterTextColor);

        /// <summary>
        ///     Sends player text to the UI.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="text">The text to send.</param>
        /// <param name="playerTextColor">The color of the player's text.</param>
        public abstract void SendPlayerText(string playerName, string text, Color playerTextColor);

        /// <summary>
        ///     Retrieves the CanvasGroup component from the UI instance.
        /// </summary>
        /// <returns>The CanvasGroup component.</returns>
        public CanvasGroup GetCanvasGroup()
        {
            return UIInstance.GetComponent<CanvasGroup>();
        }

        public static event Action<bool> UIStatusChange;

        private void SetupMarkerImage()
        {
            if (recordingMarker == null) throw new NullReferenceException("Recording Marker Image cannot be null, please assign an image for it");
            _recordingMarkerImage = recordingMarker.GetComponent<Image>();
            if (_recordingMarkerImage == null) throw new NullReferenceException("Recording Marker does not have an Image Component attached, system cannot work without it");
            _markerInitialAlpha = _recordingMarkerImage.color.a;
        }

        /// <summary>
        ///     Adds a character to the list of known characters if it does not already exist.
        /// </summary>
        /// <param name="character">The character to add.</param>
        public void AddCharacter(Character character)
        {
            if (!HasCharacter(character.characterName))
                _characters.Add(character);
        }

        /// <summary>
        ///     Checks if a character with the given name exists in the list of known characters.
        /// </summary>
        /// <param name="characterName">The name of the character to check.</param>
        /// <returns>True if the character exists, false otherwise.</returns>
        public bool HasCharacter(string characterName)
        {
            return _characters.Any(character => character.characterName == characterName);
        }

        /// <summary>
        ///     Handles the player speaking state change by updating the recording marker's visibility.
        /// </summary>
        /// <param name="isSpeaking">Whether the player is currently speaking.</param>
        private void OnPlayerSpeakingChanged(bool isSpeaking)
        {
            if (_recordingMarkerImage != null)
                _recordingMarkerImage = _recordingMarkerImage.WithColorValue(a: isSpeaking ? 1.0f : _markerInitialAlpha);
            else
                ConvaiLogger.Error("Image component not found on recording marker.", ConvaiLogger.LogCategory.Character);
        }

        /// <summary>
        ///     Sets the active state of the recording marker.
        /// </summary>
        /// <param name="active">The active state to set.</param>
        private void SetRecordingMarkerActive(bool active)
        {
            if (recordingMarker != null)
                recordingMarker.SetActive(active);
            else
                ConvaiLogger.Error("Recording marker GameObject is not assigned.", ConvaiLogger.LogCategory.Character);
        }

        /// <summary>
        ///     Sets the active state of the UI instance.
        /// </summary>
        /// <param name="active">The active state to set.</param>
        private void SetUIActive(bool active)
        {
            if (UIInstance != null)
                UIInstance.SetActive(active);
            else
                ConvaiLogger.Error("UI instance GameObject is not assigned.", ConvaiLogger.LogCategory.Character);
        }
    }
}