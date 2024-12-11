using Convai.Scripts.Runtime.LoggerSystem;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     SubtitleChatUI is responsible for displaying subtitles on the screen.
    ///     It inherits from ChatUIBase and overrides methods to provide specific functionality for subtitle UI.
    /// </summary>
    public class SubtitleChatUI : ChatUIBase
    {
        private GameObject _feedbackButtons;
        private Message _subtitle;

        /// <summary>
        ///     Initializes the subtitle UI with the provided prefab.
        /// </summary>
        /// <param name="uiPrefab">The UI prefab to instantiate.</param>
        public override void Initialize(GameObject uiPrefab)
        {
            // Instantiate the UI prefab and get the subtitle text component
            UIInstance = Instantiate(uiPrefab);
            _subtitle = new Message
            {
                SenderTextObject = UIInstance.transform.Find("Background").Find("ChatBox").Find("Subtitle").Find("Sender").GetComponent<TMP_Text>(),
                MessageTextObject = UIInstance.transform.Find("Background").Find("ChatBox").Find("Subtitle").Find("Text").GetComponent<TMP_Text>()
            };

            // Start with the UI inactive
            UIInstance.SetActive(false);

            _feedbackButtons = _subtitle.MessageTextObject.transform.GetChild(0).gameObject;
        }

        /// <summary>
        ///     Sends the character's text to the subtitle UI.
        /// </summary>
        /// <param name="charName">The name of the character speaking.</param>
        /// <param name="text">The text spoken by the character.</param>
        /// <param name="characterTextColor">The color associated with the character.</param>
        public override void SendCharacterText(string charName, string text, Color characterTextColor)
        {
            // Update the subtitle text with formatted character dialogue.
            _feedbackButtons.SetActive(false);
            UpdateSubtitleText(charName, text, characterTextColor);
            _feedbackButtons.SetActive(true);
        }

        /// <summary>
        ///     Sends the player's text to the subtitle UI.
        /// </summary>
        /// <param name="playerName">The name of the player speaking.</param>
        /// <param name="text">The text spoken by the player.</param>
        /// <param name="playerTextColor">The color associated with the player.</param>
        public override void SendPlayerText(string playerName, string text, Color playerTextColor)
        {
            // Update the subtitle text with formatted player dialogue.
            UpdateSubtitleText(playerName, text, playerTextColor);
            _feedbackButtons.SetActive(false);
        }

        /// <summary>
        ///     Updates the subtitle text with the provided speaker's name, text, and color.
        /// </summary>
        /// <param name="speakerName">The name of the speaker.</param>
        /// <param name="text">The text spoken by the speaker.</param>
        /// <param name="color">The color associated with the speaker.</param>
        private void UpdateSubtitleText(string speakerName, string text, Color color)
        {
            // Check if the subtitle text component is available before updating.
            if (_subtitle != null)
            {
                _subtitle.SenderTextObject.text = FormatSpeakerName(speakerName, color);
                _subtitle.MessageTextObject.text = text;
                _subtitle.RTLUpdate();
            }
            else
            {
                ConvaiLogger.Warn("Subtitle text component is not available.", ConvaiLogger.LogCategory.UI);
            }
        }

        /// <summary>
        ///     Formats the speaker's name with the color tag.
        /// </summary>
        /// <param name="speakerName">The name of the speaker.</param>
        /// <param name="color">The color associated with the speaker.</param>
        /// <returns>The formatted speaker name</returns>
        private string FormatSpeakerName(string speakerName, Color color)
        {
            // Convert the color to a hex string for HTML color formatting.
            string colorHex = ColorUtility.ToHtmlStringRGB(color);
            // Return the formatted text with the speaker's name and color.
            return $"<color=#{colorHex}>{speakerName}</color>: ";
        }
    }
}