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

        /// <summary>
        /// Some speech services (i.e., online services such as Azure) produce costs even when no one is speaking.
        /// Thus, we use a noise gate so that we send audio to those servers only when there's actually (or rather, probably) someone speaking.<br/>
        /// For digital captures, a very low value such as the default 0.01 should be sufficient.<br/>
        /// For analog captures, where there's always some noise, you'll want a larger value.
        /// </summary>
        /// <see cref="global::Soundbox.Audio.Processing.Noisegate.NoiseGateStreamAudioProcessorOptions.VolumeThreshold"/>
        public float VolumeThreshold { get; set; } = 0.01f;
    }
}
