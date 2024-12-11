using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     The QuestionAnswerUI class is responsible for managing the UI elements
    ///     that display questions and answers in a conversational interface.
    /// </summary>
    public class QuestionAnswerUI : ChatUIBase
    {
        private Message _answer;
        private GameObject _feedbackButtons;
        private Message _question;

        /// <summary>
        ///     Initializes the UI with the provided prefab.
        /// </summary>
        /// <param name="uiPrefab">The UI prefab to instantiate.</param>
        public override void Initialize(GameObject uiPrefab)
        {
            UIInstance = Instantiate(uiPrefab);
            _question = new Message
            {
                SenderTextObject = UIInstance.transform.Find("Background").Find("Question").Find("Sender").GetComponent<TMP_Text>(),
                MessageTextObject = UIInstance.transform.Find("Background").Find("Question").Find("Text").GetComponent<TMP_Text>()
            };
            _answer = new Message
            {
                SenderTextObject = UIInstance.transform.Find("Background").Find("Answer").Find("Sender").GetComponent<TMP_Text>(),
                MessageTextObject = UIInstance.transform.Find("Background").Find("Answer").Find("AnswerText").Find("Text").GetComponent<TMP_Text>()
            };
            UIInstance.SetActive(false);
            _feedbackButtons = _answer.MessageTextObject.transform.GetChild(0).gameObject;
        }

        /// <summary>
        ///     Sends the character's text to the UI, formatted with the character's color.
        /// </summary>
        /// <param name="charName">The name of the character speaking.</param>
        /// <param name="text">The text spoken by the character.</param>
        /// <param name="characterTextColor">The color associated with the character.</param>
        public override void SendCharacterText(string charName, string text, Color characterTextColor)
        {
            if (_answer != null)
            {
                _feedbackButtons.SetActive(false);
                _answer.SenderTextObject.text = FormatSpeakerName(charName, characterTextColor);
                _answer.MessageTextObject.text = text;
                _answer.RTLUpdate();
                _feedbackButtons.SetActive(true);
            }
        }

        /// <summary>
        ///     Sends the player's text to the UI, formatted with the player's color.
        /// </summary>
        /// <param name="playerName">The name of the player speaking.</param>
        /// <param name="text">The text spoken by the player.</param>
        /// <param name="playerTextColor">The color associated with the player.</param>
        public override void SendPlayerText(string playerName, string text, Color playerTextColor)
        {
            if (_question != null)
            {
                _question.SenderTextObject.text = FormatSpeakerName(playerName, playerTextColor);
                _question.MessageTextObject.text = text;
                _answer.RTLUpdate();
                _feedbackButtons.SetActive(false);
            }
        }

        /// <summary>
        ///     Formats the speaker's name with the color tag
        /// </summary>
        /// <param name="speakerName">The name of the speaker.</param>
        /// <param name="speakerColor">The color associated with the speaker.</param>
        /// <returns>Formatted speaker name.</returns>
        private string FormatSpeakerName(string speakerName, Color speakerColor)
        {
            string colorHex = ColorUtility.ToHtmlStringRGBA(speakerColor);
            return $"<color=#{colorHex}>{speakerName}</color>: ";
        }
    }
}