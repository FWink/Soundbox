using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Options passed to <see cref="ISpeechRecognitionService"/> to adjust and fine-tune the speech recognition.<br/>
    /// Some options are required (<see cref="Languages"/>), while some other options are optional (<see cref="Phrases"/>).
    /// </summary>
    public class SpeechRecognitionOptions
    {
        /// <summary>
        /// List of languages that should be recognized in order of preference.
        /// Format: either two-letter or five-letter language codes (e.g., "en" or "en-US"). You should prefer to use five-letter codes.
        /// </summary>
        public IList<string> Languages { get; set; }

        /// <summary>
        /// A relatively short list of special phrases that may be hard to recognize.
        /// This can vastly improve the speech recognition for words/phrases that aren't usually included in casual day-to-day speech
        /// (e.g., using "Juggernaut" here may cause the recognizer to actually recognize the word "Juggernaut" instead of "Jogger not" or such).
        /// </summary>
        public ICollection<string> Phrases { get; set; }
    }
}
