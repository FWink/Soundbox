using System.Collections.Generic;
using System.Linq;

namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionMatchState
    {
        public SpeechRecognizedEvent Event { get; set; }

        public string ResultID => Event.ResultID;

        /// <summary>
        /// Start timestamp of the paragraph.
        /// </summary>
        public long TimestampMillisStart
        {
            get
            {
                if (Previous.Count == 0)
                    return Event.TimestampMillis;
                return Previous.First().TimestampMillisStart;
            }
        }

        /// <summary>
        /// Current timestamp
        /// </summary>
        public long TimestampMillisEnd => Event.TimestampMillis;

        /// <summary>
        /// Returns the sentence's words. In sentence mode, this is just <see cref="SpeechRecognizedEvent.Words"/> from <see cref="Event"/>.
        /// In word mode, this contains the previously detected words as well (since <see cref="TimestampMillisStart"/>).
        /// </summary>
        public IList<string> Words
        {
            get
            {
                if (!Event.WordResult)
                    return Event.Words;

                var words = new List<string>();
                var prev = Previous.LastOrDefault();
                if (prev != null)
                    words.AddRange(prev.Words);
                words.AddRange(Event.Words);

                return words;
            }
        }

        private int? _wordsUsedIndex;
        /// <summary>
        /// Last index in <see cref="Words"/> that we have already used to successfully match against a <see cref="ISpeechRecognizable"/>.
        /// Thus, further matching on this sentence should start at the next index.
        /// </summary>
        public int WordsUsedIndex
        {
            get
            {
                if (_wordsUsedIndex != null)
                    return _wordsUsedIndex.Value;
                var fromPrevious = Previous?.LastOrDefault()?.WordsUsedIndex;
                if (fromPrevious != null)
                    return fromPrevious.Value;
                return -1;
            }
            set
            {
                _wordsUsedIndex = value;
            }
        }

        /// <summary>
        /// Words that have not been used to match against a <see cref="ISpeechRecognizable"/> yet: words from <see cref="Words"/> that come after <see cref="WordsUsedIndex"/>.
        /// </summary>
        public IList<string> WordsRemaining
        {
            get
            {
                var words = Words;

                var index = WordsUsedIndex;
                if (index < 0)
                    return words;
                return words.TakeLast(words.Count - 1 - index).ToList();
            }
        }

        /// <summary>
        /// Sets a new <see cref="WordsUsedIndex"/> after the given number words have been used from <see cref="WordsRemaining"/>.
        /// </summary>
        /// <param name="count"></param>
        public void AddWordsUsed(int count)
        {
            WordsUsedIndex += count;
        }

        /// <summary>
        /// Previous match states for the same sentence.
        /// </summary>
        public IList<SpeechRecognitionMatchState> Previous { get; set; } = new List<SpeechRecognitionMatchState>();
    }
}
