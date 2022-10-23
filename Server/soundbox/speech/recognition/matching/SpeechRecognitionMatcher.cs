using Microsoft.Extensions.Logging;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.Linq;

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
            //shuffle the input list: this allows us to pick a random "winner" when there are multiple recognizables with matching triggers
            foreach (var recognizable in recognizables.Shuffle())
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

            var result = new SpeechRecognitionMatchResult()
            {
                Recognizable = recognizable
            };

            var spokenNormalized = SpeechRecognitionWordNormalization.GetWordsNormalized(newState.WordsRemaining, speechEvent.Language);
            var spokenWords = spokenNormalized.NormalizedWords;
            if (spokenWords.Count == 0)
                //nothing to match
                return result;

            int iCandidate = -1;
            foreach (var triggerNormalized in GetWordsNormalized(recognizable, speechEvent.Language))
            {
                ++iCandidate;
                var triggerWords = triggerNormalized.NormalizedWords;
                if (triggerWords.Count == 0)
                    continue;

                //check if triggerWords are included in words
                for (int iWords = 0; iWords < spokenWords.Count; ++iWords)
                {
                    if (iWords + triggerWords.Count > spokenWords.Count)
                        break;

                    bool equals = true;
                    for (int iTrigger = 0; iTrigger < triggerWords.Count; ++iTrigger)
                    {
                        if (spokenWords[iWords + iTrigger] != triggerWords[iTrigger])
                        {
                            equals = false;
                            break;
                        }
                    }

                    if (equals)
                    {
                        //matched
                        Logger.LogTrace($"Result {speechEvent.ResultID}: Matched words '{recognizable.SpeechTriggers.ElementAt(iCandidate)}' in spoken '{speechEvent.Text}'[{newState.WordsUsedIndex}] ({speechEvent.Language})");

                        if (!SelectRandom(recognizable))
                        {
                            Logger.LogTrace($"Discarding match on '{recognizable.SpeechTriggers.ElementAt(iCandidate)}'. Probability check failed ({recognizable.SpeechProbability})");
                            return result;
                        }

                        result.Success = true;
                        result.WordsSpokenMatched = spokenNormalized.GetInputWords(iWords, iWords + triggerWords.Count);
                        newState.AddWordsUsed(iWords + triggerWords.Count);
                        this.State = newState;
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
            if (State != null && ReferenceEquals(State.Event, speechEvent))
            {
                //same event as last call. this has probably been called in a loop to get all matches => continue using the previous state as is
                return State;
            }

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

        #region "Random selection"

        /// <summary>
        /// Called in <see cref="Match(SpeechRecognizedEvent, ISpeechRecognizable)"/>.
        /// Runs a probability check against <see cref="ISpeechRecognizable.SpeechProbability"/> (if applicable) to decide if we should actually match this recognizable (true)
        /// or skip it (false).
        /// </summary>
        /// <param name="recognizable"></param>
        /// <returns></returns>
        protected bool SelectRandom(ISpeechRecognizable recognizable)
        {
            if (!(recognizable.SpeechProbability > 0 && recognizable.SpeechProbability < 1))
                //not set up or 100% probability
                return true;
            //this should be good enough, don't need a crypto RNG here:
            return new Random().NextDouble() < recognizable.SpeechProbability;
        }

        #endregion

        #region "Normalization"

        /// <summary>
        /// Returns the normalized words for the given recognizable
        /// </summary>
        /// <param name="recognizable"></param>
        /// <param name="language"></param>
        /// <returns></returns>
        protected IEnumerable<SpeechRecognitionNormalizedWords> GetWordsNormalized(ISpeechRecognizable recognizable, string language)
        {
            //TODO speech: cache this in the recognizable
            var triggers = recognizable.SpeechTriggers;
            if (triggers?.Count > 0)
            {
                return triggers.Select(sentence => SpeechRecognitionWordNormalization.GetWordsNormalized(SpeechRecognitionWordNormalization.ToWords(sentence, language), language));
            }
            return new SpeechRecognitionNormalizedWords[0];
        }

        #endregion
    }
}
