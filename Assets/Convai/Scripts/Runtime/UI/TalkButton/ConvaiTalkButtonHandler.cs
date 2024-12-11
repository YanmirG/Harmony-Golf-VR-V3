using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.UI
{
    public class ConvaiTalkButtonHandler : Button
    {
        private const float NORMAL_ALPHA = 1f; // The alpha when button is not pressed
        private const float PRESSED_ALPHA = 0.5f; // The alpha when button is pressed
        private ConvaiNPC _currentActiveNPC;
        private bool _subscribed;

        protected override void Start()
        {
            base.Start();
            if (ConvaiNPCManager.Instance != null)
            {
                ConvaiNPCManager.Instance.OnActiveNPCChanged += OnActiveNPCChangedHandler;
                ConvaiLogger.Info("Listening to OnActiveNPCChanged event.", ConvaiLogger.LogCategory.Character);
            }
            else
            {
                ConvaiLogger.Warn("Instance of ConvaiNPCManager is not yet initialized.", ConvaiLogger.LogCategory.Character);
            }
        }

        protected override void OnEnable()
        {
            // Check if NPC Manager instance is available before subscribing
            ConvaiNPCManager npcManager = ConvaiNPCManager.Instance;
            if (npcManager != null)
            {
                npcManager.OnActiveNPCChanged += OnActiveNPCChangedHandler;
                _currentActiveNPC = npcManager.GetActiveConvaiNPC();
                if (!_subscribed)
                {
                    _subscribed = true;
                    ConvaiLogger.Info("Subscribed to OnActiveNPCChanged event.", ConvaiLogger.LogCategory.Character);
                }
            }
            else
            {
                ConvaiLogger.Warn("NPC Manager instance is not available during enabling.", ConvaiLogger.LogCategory.Character);
            }
        }

        protected override void OnDisable()
        {
            // Always make sure to unsubscribe from events when the object is disabled
            ConvaiNPCManager npcManager = ConvaiNPCManager.Instance;
            if (npcManager != null)
            {
                npcManager.OnActiveNPCChanged -= OnActiveNPCChangedHandler;
                if (_subscribed)
                {
                    _subscribed = false;
                    ConvaiLogger.Info("Unsubscribed from OnActiveNPCChanged event.", ConvaiLogger.LogCategory.Character);
                }
            }
        }

        protected override void OnDestroy()
        {
            if (ConvaiNPCManager.Instance != null)
            {
                ConvaiNPCManager.Instance.OnActiveNPCChanged -= OnActiveNPCChangedHandler;
                ConvaiLogger.Info("Stopped listening to OnActiveNPCChanged event.", ConvaiLogger.LogCategory.Character);
            }
        }

        private void OnActiveNPCChangedHandler(ConvaiNPC newActiveNPC)
        {
            _currentActiveNPC = newActiveNPC;
            if (_currentActiveNPC != null)
                ConvaiLogger.Info($"Active NPC has changed to: {_currentActiveNPC.name}", ConvaiLogger.LogCategory.Character);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);

            ColorBlock colorBlock = colors;
            colorBlock.normalColor = new Color(colorBlock.normalColor.r, colorBlock.normalColor.g,
                colorBlock.normalColor.b,
                PRESSED_ALPHA);
            colors = colorBlock;

            if (_currentActiveNPC != null)
            {
                // _grpcAPI.InterruptCharacterSpeech();
                _currentActiveNPC.playerInteractionManager.UpdateActionConfig();
                _currentActiveNPC.StartListening();
                IncreaseScale();
                ConvaiLogger.DebugLog($"{gameObject.name} Was Clicked.", ConvaiLogger.LogCategory.Character);
            }
            else
            {
                ConvaiLogger.Warn("No active NPC found when button was pressed.", ConvaiLogger.LogCategory.Character);
            }
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);

            ColorBlock colorBlock = colors;
            colorBlock.normalColor =
                new Color(colorBlock.normalColor.r, colorBlock.normalColor.g, colorBlock.normalColor.b, NORMAL_ALPHA);
            colors = colorBlock;

            if (_currentActiveNPC != null)
            {
                _currentActiveNPC.StopListening();
                DecreaseScale();
                ConvaiLogger.DebugLog($"{gameObject.name} Was Released.", ConvaiLogger.LogCategory.Character);
            }
            else
            {
                ConvaiLogger.Warn("No active NPC found when button was released.", ConvaiLogger.LogCategory.Character);
            }
        }

        private void IncreaseScale()
        {
            Vector3 targetScale = new(1.25f, 1.25f, 1.25f);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f);
        }

        private void DecreaseScale()
        {
            Vector3 targetScale = new(1, 1, 1);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f);
        }
    }
}