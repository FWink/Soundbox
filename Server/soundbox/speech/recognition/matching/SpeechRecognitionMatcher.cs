using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Evaluates spoken words in a <see cref="SpeechRecognizedEvent"/> to search for matching <see cref="ISpeechRecognizable"/>s.
    /// Instances here are stateful and may be used for the output of exactly one <see cref="ISpeechRecognitionService"/> only.
    /// </summary>
    public class SpeechRecognitionMatcher
    {
        protected ILogger Logger;

        /// <summary>
        /// Keeps track of previous events and previous matches up to a certain point (when a paragraph has been fully detected by the recognizer -> previous state won't matter anymore).
        /// </summary>
        protected SpeechRecognitionMatchState State;

        public SpeechRecognitionMatcher(ILogger<SpeechRecognitionMatcher> logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Calls <see cref="Match(ISpeechRecognizable, SpeechRecognitionMatchState)"/> and returns the first result that matches well enough with the spoken words.
        /// </summary>
        /// <param name="speechEvent"></param>
        /// <param name="recognizables"></param>
        /// <returns></returns>
        public SpeechRecognitionMatchResult Match(SpeechRecognizedEvent speechEvent, IEnumerable<ISpeechRecognizable> recognizables)
        {
            foreach (var recognizable in recognizables)
            {
                var result = Match(speechEvent, recognizable);
                if (result.Success)
                    return result;
            }

            this.State = GetMatchState(speechEvent);
            return new SpeechRecognitionMatchResult();
        }

        /// <summary>
        /// Matches the transcribed <see cref="SpeechRecognizedEvent.Text"/> against the given recognizable's <see cref="ISpeechRecognizable.SpeechTriggers"/>.<br/>
        /// The matcher automatically keeps track of previous events (and previous matches) that may affect the detection. 
        /// This is done to avoid detecting the same trigger multiple times while the recognizer is still transcribing the full sentence (in not <see cref="SpeechRecognizedEvent.WordResult"/> mode),
        /// but also used to to puzzle single words together in <see cref="SpeechRecognizedEvent.WordResult"/> mode.
        /// </summary>
        /// <param name="speechEvent"></param>
        /// <param name="recognizable"></param>
        /// <param name="state">
        /// The <see cref="SpeechRecognitionMatchResult.State"/> from the previous matching operation.
        /// This is used to avoid detecting the same trigger multiple times while the recognizer is still transcribing the full sentence (in not <see cref="WordResult"/> mode),
        /// but also used to to puzzle single words together in <see cref="WordResult"/> mode.
        /// </param>
        /// <returns></returns>
        public SpeechRecognitionMatchResult Match(SpeechRecognizedEvent speechEvent, ISpeechRecognizable recognizable)
        {
            //TODO speech: when the recognizer switches the detected language, we might get the exact same text twice with different resultIDs. e.g.
            //(349200000|de-DE) hello there
            //(352100000|en-US) hello there
            //=> probably check on a change in language and remove words that were already included in the previous state

            var newState = GetMatchState(speechEvent);
            this.State = newState;

            var result = new SpeechRecognitionMatchResult()
            {
                Recognizable = recognizable
            };

            var words = GetWordsNormalized(newState.WordsRemaining, speechEvent.Language);
            if (words.Count == 0)
                //nothing to match
                return result;

            int iCandidate = -1;
            foreach (var triggerWords in GetWordsNormalized(recognizable, speechEvent.Language))
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
                        Logger.LogTrace($"Result {speechEvent.ResultID}: Matched words '{recognizable.SpeechTriggers.ElementAt(iCandidate)}' in spoken '{speechEvent.Text}'[{newState.WordsUsedIndex}] ({speechEvent.Language})");

                        result.Success = true;
                        newState.AddWordsUsed(iWords + triggerWords.Count);
                        return result;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a new match state for this event and the previous <see cref="State"/>
        /// </summary>
        /// <param name="recognizable"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected SpeechRecognitionMatchState GetMatchState(SpeechRecognizedEvent speechEvent)
        {
            SpeechRecognitionMatchState newState = new SpeechRecognitionMatchState()
            {
                Event = speechEvent
            };
            if (State != null && ((!speechEvent.WordResult && State.ResultID == speechEvent.ResultID) || (speechEvent.WordResult && speechEvent.TimestampMillis - State.TimestampMillisEnd < 3000)))
            {
                //keep using the previous detection state
                newState.Previous.Add(State);
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
    }
}
