using System.Collections.Generic;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Produced by <see cref="SpeechRecognitionWordNormalization.GetWordsNormalized(System.Collections.Generic.IEnumerable{string}, string)"/>:
    /// Contains both the normalized result words and the input words with a mapping that allows you to query the originally spoken words
    /// from given normalized words.
    /// </summary>
    public class SpeechRecognitionNormalizedWords
    {
        /// <summary>
        /// Result: the normalized words
        /// </summary>
        public IList<string> NormalizedWords { get; set; }

        /// <summary>
        /// Original input words.
        /// </summary>
        public IList<string> InputWords { get; set; }

        /// <summary>
        /// Returns words from <see cref="InputWords"/> that correspond to the given range of  words in <see cref="NormalizedWords"/>.
        /// </summary>
        /// <param name="fromNormalized"></param>
        /// <param name="toNormalized"></param>
        /// <returns></returns>
        public IList<string> GetInputWords(int fromNormalized, int toNormalized)
        {
            var words = new List<string>();
            for (int i = fromNormalized; i < toNormalized; ++i)
            {
                words.Add(InputWords[i]);
            }
            return words;
        }
    }
}
