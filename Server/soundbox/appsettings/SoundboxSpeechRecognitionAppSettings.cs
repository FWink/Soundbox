using System.Collections.Generic;

namespace Soundbox.AppSettings
{
    /// <summary>
    /// Soundbox app settings with parameters for speech recognition.<br/>
    /// Does not contain <see cref="Speech.Recognition.AppSettings.SpeechRecognitionAppSettings"/>,
    /// as those are global configurations (i.e., on a dependency injection level). Here we have settings specific to the soundbox's
    /// 24/7 speech recognition, while other usages of the speech recognition might require other settings (e.g., when testing the speech recognition while editing/uploading sounds).
    /// </summary>
    public class SoundboxSpeechRecognitionAppSettings
    {
        /// <summary>
        /// Input audio device.
        /// </summary>
        public Audio.AudioDevice AudioDevice { get; set; }

        /// <summary>
        /// See <see cref="global::Soundbox.Speech.Recognition.SpeechRecognitionConfig.VolumeThreshold"/> and <see cref="global::Soundbox.Audio.Processing.Noisegate.NoiseGateStreamAudioProcessorOptions.VolumeThreshold"/><br/>
        /// The noise gate implementation will occassionally write detected peak levels into the logs, so that should give you an idea where to start.
        /// </summary>
        public float VolumeThreshold { get; set; } = 0.01f;

        /// <summary>
        /// See <see cref="Speech.Recognition.SpeechRecognitionOptions.Languages"/>
        /// </summary>
        public IList<string> Languages { get; set; }
    }
}
