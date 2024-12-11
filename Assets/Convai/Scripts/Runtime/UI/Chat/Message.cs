using Convai.Scripts.Runtime.Utils;
using TMPro;

namespace Convai.Scripts.Runtime.UI
{
    /// <summary>
    ///     Class to keep track of individual chat messages.
    /// </summary>
    public class Message
    {
        public TMP_Text SenderTextObject { get; set; }
        public TMP_Text MessageTextObject { get; set; }

        /// <summary>
        ///     Does an RTL check for the message and changes the order of sender-text in the UI if both are in an
        ///     RTL Language
        /// </summary>
        public void RTLUpdate()
        {
            // Enable the RTL on the Sender Component
            if (ConvaiLanguageCheck.IsRTL(SenderTextObject.text)) MessageTextObject.isRightToLeftText = true;

            // Enable the RTL on the Text Component
            if (ConvaiLanguageCheck.IsRTL(MessageTextObject.text)) MessageTextObject.isRightToLeftText = true;
        }
    }
}