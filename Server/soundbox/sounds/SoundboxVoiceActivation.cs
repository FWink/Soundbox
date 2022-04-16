using Soundbox.Speech.Recognition;
using System.Collections.Generic;

namespace Soundbox
{
    /// <summary>
    /// Contains voice-activation settings for a single sound.
    /// </summary>
    public class SoundboxVoiceActivation : ISpeechRecognizable
    {
        private ICollection<string> _speechTriggers;
        public ICollection<string> SpeechTriggers
        {
            get
            {
                if (_speechTriggers == null)
                    _speechTriggers = new List<string>();
                return _speechTriggers;
            }
            set
            {
                _speechTriggers = value;
            }
        }

        private ICollection<string> _speechPhrases;
        /// <summary>
        /// Special words or phrases included in <see cref="ISpeechRecognizable.SpeechTriggers" /> that are hard to detect:
        /// you wouldn't usually expect a speech recognition software to be able to detect these words.
        /// By specifying them here, we can help the speech recognition and tell it to look specifically for these words.
        /// </summary>
        /// <seealso cref="SpeechRecognitionOptions.Phrases"/>
        public ICollection<string> SpeechPhrases
        {
            get
            {
                if (_speechPhrases == null)
                    _speechPhrases = new List<string>();
                return _speechPhrases;
            }
            set
            {
                _speechPhrases = value;
            }
        }

        public SoundboxVoiceActivation()
        {
        }

        /// <summary>
        /// Performs a deep copy.
        /// </summary>
        /// <param name="other"></param>
        public SoundboxVoiceActivation(SoundboxVoiceActivation other)
        {
            if (other._speechTriggers != null)
                _speechTriggers = new List<string>(other._speechTriggers);
            if (other._speechPhrases != null)
                _speechPhrases = new List<string>(other._speechPhrases);
        }
    }
}
