using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Generated when a <see cref="ISpeechRecognitionService"/> successfully recognized and transcribed spoken words.<br/>
    /// Results here may be <see cref="Preliminary"/>, as the speech recognizer might not work word-for-word, but on entire sentences and paragraphs.
    /// Thus, events here may represent preliminary results for a part of a sentence, but the speech recognizer might settle on a better translation once the spoken paragraph is complete.
    /// For that purpose, all related events share a same <see cref="ResultID"/>: events with the same <see cref="ResultID"/> have the same start point in the audio recording,
    /// but newer events may include additional words as they are being spoken. A chain of events with the same <see cref="ResultID"/> might look like this:<list type="number">
    /// <item>Lorem</item>
    /// <item>Lorem ipsum</item>
    /// <item>Lorem ipsum dohor [sic]</item>
    /// <item>Lorem ipsum dolor sit [previous word got changed as the speech recognizer settled on a different word]</item>
    /// <item>Lorem ipsum dolor sit amet</item>
    /// <item>Lorem ipsum dolor sit amet [final result]</item>
    /// </list>
    /// However, some speech recognizers may indeed work in a word-for-word mode (<see cref="WordResult"/>),
    /// then you'd get every word or short bursts of words delivered separately.
    /// </summary>
    public class SpeechRecognizedEvent : SpeechRecognitionBaseEvent
    {
        /// <summary>
        /// Not unique per event, but per transcribed group of words.
        /// </summary>
        public string ResultID { get; set; }
        /// <summary>
        /// True: the transcribed text is not final and words that have been recognized already may change with further events for this <see cref="ResultID"/>.
        /// </summary>
        public bool Preliminary { get; set; }
        /// <summary>
        /// True: the speech recognizer works in a word-for-word mode as opposed to transcribing entire sentences or paragraphs in one go.
        /// </summary>
        public bool WordResult { get; set; }
        /// <summary>
        /// The entire transcribed text.
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// Optional: detected language.
        /// Might not be in the exact format that was passed to the recognizer via <see cref="SpeechRecognitionOptions.Languages"/>.
        /// Should expect two-letter and five-letter codes here.
        /// </summary>
        public string Language { get; set; }
        /// <summary>
        /// <see cref="Text"/> split into individual words.
        /// </summary>
        public IList<string> Words => SpeechRecognitionMatcher.ToWords(Text, Language);
    }
}
