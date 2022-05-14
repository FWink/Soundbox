using System.Collections.Generic;

namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionMatchResult
    {
        public ISpeechRecognizable Recognizable { get; set; }

        /// <summary>
        /// True: the recognizable matches the spoken text exactly or close enough.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// On <see cref="Success"/>: the spoken words that were detected as a match for the <see cref="Recognizable"/>.
        /// Note that these are in fact the recognized *spoken* words as opposed to the defined <see cref="ISpeechRecognizable.SpeechTriggers"/> of the recognizable.
        /// </summary>
        public IList<string> WordsSpokenMatched { get; set; }
    }
}
