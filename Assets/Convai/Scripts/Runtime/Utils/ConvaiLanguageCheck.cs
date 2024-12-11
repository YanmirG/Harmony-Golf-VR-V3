using System.Linq;

namespace Convai.Scripts.Runtime.Utils
{
    /// <summary>
    ///     Utility for language features
    /// </summary>
    public abstract class ConvaiLanguageCheck
    {
        private const char MIN_ARABIC_RANGE = '\u0600';
        private const char MAX_ARABIC_RANGE = '\u06FF';

        /// <summary>
        ///     To check if the text is in a Right-to-Left(RTL) Language
        /// </summary>
        /// <param name="text">The text for RTL Check.</param>
        /// <returns>Bool denoting the text is RTL or not</returns>
        public static bool IsRTL(string text)
        {
            return text.Any(c => c is >= MIN_ARABIC_RANGE and <= MAX_ARABIC_RANGE);
        }
    }
}