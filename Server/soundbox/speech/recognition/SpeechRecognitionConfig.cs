using Soundbox.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Configuration passed to <see cref="ISpeechRecognitionServiceProvider"/>
    /// </summary>
    public class SpeechRecognitionConfig
    {
        /// <summary>
        /// Audio source for the speech recognition.
        /// </summary>
        public IAudioSource AudioSource { get; set; }
    }
}
