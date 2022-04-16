using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        protected ILogger Logger;

        public SpeechRecognizedEvent(ILogger<SpeechRecognizedEvent> logger)
        {
            this.Logger = logger;
        }

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
        public IList<string> Words => ToWords(Text, Language);

        #region "Matching"

        /// <summary>
        /// Calls <see cref="Match(ISpeechRecognizable, SpeechRecognitionMatchState)"/> and returns the first result that matches well enough with the spoken words.
        /// </summary>
        /// <param name="recognizables"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public SpeechRecognitionMatchResult Match(IEnumerable<ISpeechRecognizable> recognizables, SpeechRecognitionMatchState state = null)
        {
            foreach (var recognizable in recognizables)
            {
                var result = Match(recognizable, state);
                if (result.Success)
                    return result;
            }

            return new SpeechRecognitionMatchResult()
            {
                State = GetMatchState(state)
            };
        }

        /// <summary>
        /// Matches the transcribed <see cref="Text"/> against the given recognizable's <see cref="ISpeechRecognizable.SpeechTriggers"/>.
        /// </summary>
        /// <param name="recognizable"></param>
        /// <param name="state">
        /// The <see cref="SpeechRecognitionMatchResult.State"/> from the previous matching operation.
        /// This is used to avoid detecting the same trigger multiple times while the recognizer is still transcribing the full sentence (in not <see cref="WordResult"/> mode),
        /// but also used to to puzzle single words together in <see cref="WordResult"/> mode.
        /// </param>
        /// <returns></returns>
        public SpeechRecognitionMatchResult Match(ISpeechRecognizable recognizable, SpeechRecognitionMatchState state = null)
        {
            //TODO speech: when the recognizer switches the detected language, we might get the exact same text twice with different resultIDs. e.g.
            //(349200000|de-DE) hello there
            //(352100000|en-US) hello there
            //=> probably check on a change in language and remove words that were already included in the previous state

            var newState = GetMatchState(state);

            var result = new SpeechRecognitionMatchResult()
            {
                State = newState,
                Recognizable = recognizable
            };

            var words = GetWordsNormalized(newState.WordsRemaining, Language);
            if (words.Count == 0)
                //nothing to match
                return result;

            int iCandidate = -1;
            foreach (var triggerWords in GetWordsNormalized(recognizable, Language))
            {
                ++iCandidate;
                if (triggerWords.Count == 0)
                    continue;

                //check if triggerWords are included in words
                for (int iWords = 0; iWords < words.Count; ++iWords)
                {
                    if (iWords + triggerWords.Count > words.Count)
                        break;

                    bool equals = true;
                    for (int iTrigger = 0; iTrigger < triggerWords.Count; ++iTrigger)
                    {
                        if (words[iWords + iTrigger] != triggerWords[iTrigger])
                        {
                            equals = false;
                            break;
                        }
                    }

                    if (equals)
                    {
                        //matched
                        Logger.LogTrace($"Result {ResultID}: Matched words '{recognizable.SpeechTriggers.ElementAt(iCandidate)}' in spoken '{Text}'[{newState.WordsUsedIndex}] ({Language})");

                        result.Success = true;
                        newState.AddWordsUsed(iWords + triggerWords.Count);
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a new match state for this event and the given previous state (optional).
        /// </summary>
        /// <param name="recognizable"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected SpeechRecognitionMatchState GetMatchState(SpeechRecognitionMatchState state)
        {
            SpeechRecognitionMatchState newState = new SpeechRecognitionMatchState()
            {
                Event = this
            };
            if (state != null && ((!WordResult && state.ResultID == ResultID) || (WordResult && TimestampMillis - state.TimestampMillisEnd < 3000)))
            {
                //keep using the previous detection state
                newState.Previous.Add(state);
            }

            return newState;
        }

        #region "Normalization"

        /// <summary>
        /// Returns the normalized words for the given recognizable
        /// </summary>
        /// <param name="recognizable"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static IEnumerable<IList<string>> GetWordsNormalized(ISpeechRecognizable recognizable, string language)
        {
            //TODO speech: cache this in the recognizable
            var triggers = recognizable.SpeechTriggers;
            if (triggers?.Count > 0)
            {
                return triggers.Select(sentence => GetWordsNormalized(ToWords(sentence, language), language));
            }
            return new IList<string>[0];
        }

        protected static readonly Regex NormalizationGermanSuffixRegex = new Regex("(e[snmr]?|s)$");

        /// <summary>
        /// Normalizes the given words for matching.
        /// May or may not take the given language into account (e.g., in German language, this may remove suffixes from words to make them comparable when used in different contexts).
        /// </summary>
        /// <param name="words"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static IList<string> GetWordsNormalized(IEnumerable<string> words, string language)
        {
            var culture = System.Globalization.CultureInfo.CurrentUICulture;
            if (language != null)
                culture = System.Globalization.CultureInfo.GetCultureInfo(language);
            bool english = language != null && language.StartsWith("en");
            bool german = language != null && language.StartsWith("de");

            var normalized = new List<string>();
            foreach (var word in words)
            {
                var wordNormalized = word.ToLower(culture);

                if (english)
                {
                    wordNormalized = wordNormalized.Replace("'s", "s").Replace("'re", "r");
                }
                else if (german)
                {
                    wordNormalized = NormalizationGermanSuffixRegex.Replace(wordNormalized, "");
                    wordNormalized = wordNormalized.Replace("ß", "ss");
                }

                normalized.Add(wordNormalized);
            }

            return normalized;
        }

        #endregion

        private static readonly Regex PunctuationRegex = new Regex("[.\\-_?!]");

        /// <summary>
        /// Turns the given sentence into separate words and removes punctuations.
        /// </summary>
        /// <param name="sentence"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        public static IList<string> ToWords(string sentence, string language)
        {
            string cleaned = PunctuationRegex.Replace(sentence, " ");
            return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        #endregion
    }
}
