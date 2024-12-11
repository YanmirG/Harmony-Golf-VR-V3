using System;
using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.LoggerSystem;
using Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    public class ConvaiFeedbackHandler : MonoBehaviour
    {
        [SerializeField] private Button _thumbsUPButton;
        [SerializeField] private Button _thumbsDownButton;

        [SerializeField] private GameObject _thumbsUPFill;
        [SerializeField] private GameObject _thumbsDownFill;
        [SerializeField] private TextMeshProUGUI _feedbackText;

        private string _feedbackTextString;

        private string _interactionID;

        /// <summary>
        ///     Called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            ConvaiGRPCAPI.Instance.OnResultReceived += ConvaiGRPCAPI_OnResultReceived;
            _thumbsUPButton.onClick.AddListener(() => OnFeedbackButtonClicked(_thumbsUPButton));
            _thumbsDownButton.onClick.AddListener(() => OnFeedbackButtonClicked(_thumbsDownButton));
        }

        /// <summary>
        ///     Called when the object is disabled.
        /// </summary>
        private void OnDisable()
        {
            ConvaiGRPCAPI.Instance.OnResultReceived -= ConvaiGRPCAPI_OnResultReceived;
            _thumbsUPButton.onClick.RemoveAllListeners();
            _thumbsDownButton.onClick.RemoveAllListeners();

            _thumbsUPFill.SetActive(false);
            _thumbsDownFill.SetActive(false);
        }

        /// <summary>
        ///     Handles the event when a result is received from ConvaiGRPCAPI.
        /// </summary>
        /// <param name="result">The result received.</param>
        private void ConvaiGRPCAPI_OnResultReceived(GetResponseResponse result)
        {
            // Check if InteractionId is not null or empty.
            if (result.InteractionId.Length > 0)
            {
                _interactionID = result.InteractionId;
                ConvaiGRPCAPI.Instance.OnResultReceived -= ConvaiGRPCAPI_OnResultReceived;
            }
        }

        /// <summary>
        ///     Handles the event when the feedback button is clicked.
        /// </summary>
        private void OnFeedbackButtonClicked(Button button)
        {
            if (button == _thumbsUPButton)
                SendFeedback(true);
            else if (button == _thumbsDownButton) SendFeedback(false);
        }

        /// <summary>
        ///     Sends feedback to ConvaiGRPCAPI asynchronously.
        /// </summary>
        /// <param name="thumbsUP">Indicates whether the feedback is a thumbs up or thumbs down.</param>
        private async void SendFeedback(bool thumbsUP)
        {
            if (string.IsNullOrEmpty(_interactionID))
            {
                ConvaiLogger.Error("InteractionId is null or empty", ConvaiLogger.LogCategory.Character);
                return;
            }

            // Set the fill visuals for thumbs up and thumbs down buttons.
            HandleThumbsFill(thumbsUP);

            // Extract feedback text after the colon character.
            string feedbackText = RemoveBeforeColon(_feedbackText.text);

            // Send feedback to ConvaiGRPCAPI.
            await ConvaiGRPCAPI.Instance.SendFeedback(thumbsUP, _interactionID, feedbackText);
        }

        /// <summary>
        ///     Removes the text before the colon character in the given string.
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>The modified text after removing the portion before the colon.</returns>
        private string RemoveBeforeColon(string text)
        {
            int colonIndex = text.IndexOf(':', StringComparison.Ordinal);
            if (colonIndex != -1) return text.Substring(colonIndex + 2);

            return text;
        }

        /// <summary>
        ///     Sets the fill state of the Thumbs Up and Thumbs Down buttons.
        /// </summary>
        /// <param name="thumbsUP">Indicates whether the feedback is a thumbs up or thumbs down.</param>
        private void HandleThumbsFill(bool thumbsUP)
        {
            _thumbsUPFill.SetActive(thumbsUP);
            _thumbsDownFill.SetActive(!thumbsUP);
        }
    }
}