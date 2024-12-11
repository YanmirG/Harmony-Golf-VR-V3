using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ColorUtility = UnityEngine.ColorUtility;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     Manages the chat UI for displaying messages from characters and the player.
    /// </summary>
    public class ChatBoxUI : ChatUIBase
    {
        private const int MAX_MESSAGES = 25;
        [SerializeField] private GameObject _playerMessageObject, _characterMessageObject;

        private readonly List<Message> _messageList = new();

        private GameObject _chatPanel;
        private ScrollRect _chatScrollRect;
        private Speaker _currentSpeaker;
        private bool _isFirstMessage = true;
        private bool _isNewMessage;

        /// <summary>
        ///     Initializes the chat UI with the specified prefab.
        /// </summary>
        /// <param name="uiPrefab">The UI prefab to instantiate.</param>
        public override void Initialize(GameObject uiPrefab)
        {
            UIInstance = Instantiate(uiPrefab);
            _chatPanel = UIInstance.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject;
            _chatScrollRect = UIInstance.transform.GetChild(0).GetChild(0).GetComponent<ScrollRect>();
            _isFirstMessage = true;
            _currentSpeaker = Speaker.Player;
        }

        /// <summary>
        ///     Sends a message as a character.
        /// </summary>
        /// <param name="charName">The name of the character.</param>
        /// <param name="text">The message text.</param>
        /// <param name="characterTextColor">The color of the character's text.</param>
        public override void SendCharacterText(string charName, string text, Color characterTextColor)
        {
            BroadcastCharacterDialogue(charName, text, characterTextColor);
        }

        /// <summary>
        ///     Sends a message as the player.
        /// </summary>
        /// <param name="playerName">The name of the player.</param>
        /// <param name="text">The message text.</param>
        /// <param name="playerTextColor">The color of the player's text.</param>
        public override void SendPlayerText(string playerName, string text, Color playerTextColor)
        {
            BroadcastPlayerDialogue(playerName, text, playerTextColor);
        }

        /// <summary>
        ///     Clears all messages from the UI.
        /// </summary>
        public void ClearUI()
        {
            foreach (Message message in _messageList)
            {
                Destroy(message.SenderTextObject.gameObject);
                Destroy(message.MessageTextObject.gameObject);
            }

            _messageList.Clear();
        }

        // Helper methods and private functions are below. These are not part of the public API
        // and are used internally by the ChatBoxUI class to manage chat messages.

        /// <summary>
        ///     Broadcasts a dialogue message from a character.
        /// </summary>
        /// <param name="characterName">Name of the character.</param>
        /// <param name="text">Text of the dialogue message.</param>
        /// <param name="characterTextColor">Color of the character's text.</param>
        private void BroadcastCharacterDialogue(string characterName, string text, Color characterTextColor)
        {
            string trimmedText = text.Trim();

            if (_currentSpeaker != Speaker.Character || _isFirstMessage)
            {
                _isFirstMessage = false;
                _currentSpeaker = Speaker.Character;
                HandleNewCharacterMessage(characterName, trimmedText, characterTextColor);
            }
            else
            {
                AppendToExistingMessage(trimmedText);
            }

            _currentSpeaker = Speaker.Character;
            ScrollToBottom();
        }

        /// <summary>
        ///     Handles a new dialogue message from a character.
        /// </summary>
        /// <param name="characterName">Name of the character.</param>
        /// <param name="text">Text of the dialogue message.</param>
        /// <param name="characterTextColor">Color of the character's text.</param>
        private void HandleNewCharacterMessage(string characterName, string text, Color characterTextColor)
        {
            if (_messageList.Count >= MAX_MESSAGES) DestroyOldestMessage();

            CreateNewMessage(text, characterName, characterTextColor, false);
        }


        /// <summary>
        ///     Broadcasts a dialogue message from a player.
        /// </summary>
        /// <param name="playerName">Name of the player.</param>
        /// <param name="text">Text of the dialogue message.</param>
        /// <param name="playerTextColor">Color of the player's text.</param>
        private void BroadcastPlayerDialogue(string playerName, string text, Color playerTextColor)
        {
            string trimmedText = text.Trim();

            if (_currentSpeaker != Speaker.Player || !_messageList.Any())
            {
                _currentSpeaker = Speaker.Player;
                HandleNewPlayerMessage(playerName, trimmedText, playerTextColor);
            }
            else
            {
                ReplaceExistingPlayerMessage(playerName, trimmedText, playerTextColor);
            }

            _currentSpeaker = Speaker.Player;
            ScrollToBottom();
        }


        /// <summary>
        ///     Handles a new dialogue message from a player.
        /// </summary>
        /// <param name="playerName">Name of the player.</param>
        /// <param name="text">Text of the dialogue message.</param>
        /// <param name="playerTextColor">Color of the player's text.</param>
        private void HandleNewPlayerMessage(string playerName, string text, Color playerTextColor)
        {
            if (_messageList.Count >= MAX_MESSAGES) DestroyOldestMessage();

            CreateNewMessage(text, playerName, playerTextColor, true);
        }


        /// <summary>
        ///     Replaces an existing player message with a new one.
        /// </summary>
        /// <param name="playerName">Name of the player.</param>
        /// <param name="text">New text of the dialogue message.</param>
        /// <param name="playerTextColor">Color of the player's text.</param>
        private void ReplaceExistingPlayerMessage(string playerName, string text, Color playerTextColor)
        {
            Message lastMessage = _messageList[^1];
            lastMessage.MessageTextObject.text = text;
            lastMessage.SenderTextObject.text = FormatSpeakerName(playerName, playerTextColor);

            // RTL Update done due to text arriving after create message
            // Once for every message
            if (text != "" && _isNewMessage)
            {
                lastMessage.RTLUpdate();
                _isNewMessage = false;
            }
        }


        /// <summary>
        ///     Appends a text to the existing message.
        /// </summary>
        /// <param name="text">Text which needs to append to the existing message.</param>
        private void AppendToExistingMessage(string text)
        {
            if (_messageList.Count > 0)
            {
                Message lastMessage = _messageList[^1];
                lastMessage.MessageTextObject.text += " " + text;

                if (text != "" && _isNewMessage)
                {
                    lastMessage.RTLUpdate();
                    _isNewMessage = false;
                }
            }
        }


        /// <summary>
        /// </summary>
        private void ScrollToBottom()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_chatPanel.GetComponent<RectTransform>());
            Canvas.ForceUpdateCanvases();
            _chatScrollRect.verticalNormalizedPosition = 0f;
        }

        /// <summary>
        ///     Formats the speaker name
        /// </summary>
        /// <param name="speakerName">Name of the speaker.</param>
        /// <param name="speakerColor">Color of the speaker's text.</param>
        /// <returns>Formatted speaker's name with color tag.</returns>
        private static string FormatSpeakerName(string speakerName, Color speakerColor)
        {
            string speakerColorHtml = ColorUtility.ToHtmlStringRGB(speakerColor);
            return $"<color=#{speakerColorHtml}>{speakerName}</color>";
        }


        /// <summary>
        ///     Destroys the oldest message in the chat UI.
        /// </summary>
        private void DestroyOldestMessage()
        {
            Destroy(_messageList[0].SenderTextObject.gameObject);
            Destroy(_messageList[0].MessageTextObject.gameObject);
            _messageList.RemoveAt(0);
        }


        /// <summary>
        ///     Creates a new dialogue message.
        /// </summary>
        /// <param name="text">Text of the dialogue message.</param>
        /// <param name="speakerName">Name of the speaker.</param>
        /// <param name="speakerColor">Color of the speaker's text.</param>
        /// <param name="isSpeakerPlayer"> Flag to check if the speaker is a player.</param>
        private void CreateNewMessage(string text, string speakerName, Color speakerColor, bool isSpeakerPlayer)
        {
            _isNewMessage = true;

            GameObject messageInstance = isSpeakerPlayer ? Instantiate(_playerMessageObject, _chatPanel.transform) : Instantiate(_characterMessageObject, _chatPanel.transform);
            messageInstance.SetActive(true);

            Transform container = messageInstance.transform.Find("Container");
            Transform senderBox = container.Find("SenderBox");

            Message newMessage = new()
            {
                SenderTextObject = senderBox.Find("Sender").GetComponent<TMP_Text>(),
                MessageTextObject = container.Find("Message").GetComponent<TMP_Text>()
            };

            newMessage.SenderTextObject.text = FormatSpeakerName(speakerName, speakerColor);
            newMessage.MessageTextObject.text = text;

            _messageList.Add(newMessage);
        }

        /// <summary>
        ///     Enumeration of the possible speakers.
        /// </summary>
        private enum Speaker
        {
            Player,
            Character
        }
    }
}