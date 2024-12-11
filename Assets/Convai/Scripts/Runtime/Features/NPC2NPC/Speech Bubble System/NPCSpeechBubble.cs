using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.Features
{
    public class NPCSpeechBubble : MonoBehaviour, ISpeechBubbleDisplay
    {
        [SerializeField] private TMP_Text speechBubbleText;
        [SerializeField] private Canvas speechBubbleCanvas;

        /// <summary>
        ///     Show the speech bubble with the given text.
        /// </summary>
        /// <param name="text"> The text to display in the speech bubble. </param>
        public void ShowSpeechBubble(string text)
        {
            speechBubbleText.text = text;
            speechBubbleCanvas.enabled = true;
        }

        /// <summary>
        ///     Hide the speech bubble.
        /// </summary>
        public void HideSpeechBubble()
        {
            speechBubbleCanvas.enabled = false;
        }
    }
}