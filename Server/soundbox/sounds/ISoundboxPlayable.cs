using Newtonsoft.Json;
using Soundbox.Speech.Recognition;
using System.Collections.Generic;

namespace Soundbox
{
    /// <summary>
    /// Represents something the soundbox can play: a <see cref="Sound"/> or a macro (consisting of <see cref="Sound"/>s).
    /// This is usually also a <see cref="SoundboxNode"/>.
    /// </summary>
    public interface ISoundboxPlayable : ISpeechRecognizable
    {
        #region "Speech Recognition"
        /// <summary>
        /// Enables voice-activation/speech-recognition on this playable: saying a word or sentence in voice chat
        /// causes this playable to be played automatically by the server.
        /// </summary>
        SoundboxVoiceActivation VoiceActivation { get; set; }

        [JsonIgnore]
        ICollection<string> ISpeechRecognizable.SpeechTriggers => VoiceActivation?.SpeechTriggers;

        #endregion
    }
}
