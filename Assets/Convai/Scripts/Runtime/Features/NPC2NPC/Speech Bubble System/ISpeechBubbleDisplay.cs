namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     Interface for displaying speech bubbles.
    /// </summary>
    public interface ISpeechBubbleDisplay
    {
        void ShowSpeechBubble(string text);
        void HideSpeechBubble();
    }
}