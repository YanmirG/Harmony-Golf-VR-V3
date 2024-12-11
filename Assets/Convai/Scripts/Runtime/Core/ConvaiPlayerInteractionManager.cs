using System;
using System.Linq;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    public class ConvaiPlayerInteractionManager : MonoBehaviour
    {
        private ConvaiChatUIHandler _convaiChatUIHandler;
        private ConvaiCrosshairHandler _convaiCrosshairHandler;
        private ConvaiNPC _convaiNPC;
        private TMP_InputField _currentInputField;

        private ConvaiInputManager InputManager => ConvaiInputManager.Instance ? ConvaiInputManager.Instance : null;

        private void OnEnable()
        {
            if (InputManager == null) return;
            InputManager.sendText += HandleSendText;
            InputManager.toggleChat += HandleToggleChat;
            InputManager.talkKeyInteract += HandleVoiceInput;
            InputManager.talkKeyInteract += HandleNPCInteraction;
        }

        private void OnDisable()
        {
            if (InputManager == null) return;
            InputManager.sendText -= HandleSendText;
            InputManager.toggleChat -= HandleToggleChat;
            InputManager.talkKeyInteract -= HandleVoiceInput;
            InputManager.talkKeyInteract -= HandleNPCInteraction;
        }

        public void Initialize(ConvaiNPC convaiNPC, ConvaiCrosshairHandler convaiCrosshairHandler, ConvaiChatUIHandler convaiChatUIHandler)
        {
            _convaiNPC = convaiNPC ? convaiNPC : throw new ArgumentNullException(nameof(convaiNPC));
            _convaiCrosshairHandler = convaiCrosshairHandler ? convaiCrosshairHandler : throw new ArgumentNullException(nameof(convaiCrosshairHandler));
            _convaiChatUIHandler = convaiChatUIHandler ? convaiChatUIHandler : throw new ArgumentNullException(nameof(convaiChatUIHandler));
        }

        private void UpdateCurrentInputField(TMP_InputField inputFieldInScene)
        {
            if (inputFieldInScene != null && _currentInputField != inputFieldInScene) _currentInputField = inputFieldInScene;
        }

        private void HandleInputSubmission(string input)
        {
            if (!_convaiNPC.isCharacterActive || string.IsNullOrEmpty(input.Trim())) return;
            _convaiNPC.InterruptCharacterSpeech();
            UpdateActionConfig();
            _convaiNPC.SendTextDataAsync(input);
            _convaiChatUIHandler.SendPlayerText(input);
            ClearInputField();
        }

        public TMP_InputField FindActiveInputField()
        {
            // TODO : Implement Text Send for ChatUIBase and get input field directly instead of finding here
            return _convaiChatUIHandler.GetCurrentUI().GetCanvasGroup().gameObject.GetComponentsInChildren<TMP_InputField>(true)
                .FirstOrDefault(inputField => inputField.interactable);
        }

        private void ClearInputField()
        {
            if (_currentInputField != null)
            {
                _currentInputField.text = string.Empty;
                _currentInputField.DeactivateInputField();
            }
        }

        private void HandleToggleChat()
        {
            TMP_InputField inputFieldInScene = FindActiveInputField();
            if (!inputFieldInScene.isFocused && _convaiNPC.isCharacterActive)
            {
                inputFieldInScene.ActivateInputField();
            }
        }

        private void HandleSendText()
        {
            TMP_InputField inputFieldInScene = FindActiveInputField();

            UpdateCurrentInputField(inputFieldInScene);
            if (_currentInputField != null && _currentInputField.isFocused && _convaiNPC.isCharacterActive) HandleInputSubmission(_currentInputField.text);
        }

        private void HandleVoiceInput(bool listenState)
        {
            if (UIUtilities.IsAnyInputFieldFocused() || !_convaiNPC.isCharacterActive) return;
            switch (listenState)
            {
                case true:
                    _convaiNPC.InterruptCharacterSpeech();
                    UpdateActionConfig();
                    _convaiNPC.StartListening();
                    break;
                case false:
                {
                    if (_convaiNPC.isCharacterActive && (_currentInputField == null || !_currentInputField.isFocused)) _convaiNPC.StopListening();
                    break;
                }
            }
        }

        private void HandleNPCInteraction(bool state)
        {
            if (!IsNpcInConversation() || !state || UIUtilities.IsAnyInputFieldFocused()) return;
            NPC2NPCConversationManager.Instance.EndConversation(_convaiNPC.GetComponent<ConvaiGroupNPCController>());
            _convaiNPC.InterruptCharacterSpeech();
            _convaiNPC.StartListening();
        }

        private bool IsNpcInConversation()
        {
            bool isNpcInConversation;
            if (TryGetComponent(out ConvaiGroupNPCController convaiGroupNPC))
                isNpcInConversation = convaiGroupNPC.IsInConversationWithAnotherNPC && ConvaiNPCManager.Instance.nearbyNPC == _convaiNPC;
            else
                isNpcInConversation = false;
            return isNpcInConversation;
        }

        public void UpdateActionConfig()
        {
            if (_convaiNPC.ActionConfig != null && _convaiCrosshairHandler != null)
                _convaiNPC.ActionConfig.CurrentAttentionObject = _convaiCrosshairHandler.FindPlayerReferenceObject();
        }
    }
}