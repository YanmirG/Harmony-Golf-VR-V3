using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    public class ConvaiSpeechBubbleController : MonoBehaviour
    {
        private ConvaiGroupNPCController _convaiGroupNPC;
        private NPCSpeechBubble _speechBubble;

        private void OnDestroy()
        {
            _convaiGroupNPC.ShowSpeechBubble -= ConvaiNPC_ShowSpeechBubble;
            _convaiGroupNPC.HideSpeechBubble -= ConvaiNPC_HideSpeechBubble;
            Destroy(_speechBubble.gameObject);
            _speechBubble = null;
        }

        public void Initialize(NPCSpeechBubble speechBubbleDisplay, ConvaiGroupNPCController convaiGroupNPC)
        {
            if (_speechBubble != null) return;
            _speechBubble = Instantiate(speechBubbleDisplay, transform);
            _convaiGroupNPC = convaiGroupNPC;
            _convaiGroupNPC.ShowSpeechBubble += ConvaiNPC_ShowSpeechBubble;
            _convaiGroupNPC.HideSpeechBubble += ConvaiNPC_HideSpeechBubble;
        }

        private void ConvaiNPC_HideSpeechBubble()
        {
            _speechBubble.HideSpeechBubble();
        }

        private void ConvaiNPC_ShowSpeechBubble(string text)
        {
            if(!string.IsNullOrEmpty(text)) _speechBubble.ShowSpeechBubble(text);
        }
    }
}