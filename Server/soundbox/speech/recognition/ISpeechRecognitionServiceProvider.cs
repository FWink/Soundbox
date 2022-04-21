using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Factory that provides instances of <see cref="ISpeechRecognitionService"/> for given <see cref="SpeechRecognitionConfig"/>s.
    /// This service provider makes sure that only speech recognizers are returned that match the given config
    /// (for example, if <see cref="SpeechRecognitionConfig.AudioSource"/> is a <see cref="Audio.AudioDevice"/> but only <see cref="Audio.IStreamAudioSource"/> is supported,
    /// then the service provider would return null).
    /// </summary>
    public interface ISpeechRecognitionServiceProvider
    {
        /// <summary>
        /// Constructs a <see cref="ISpeechRecognitionService"/> for the given config,
        /// but may return null if the given config is not supported.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        ISpeechRecognitionService GetSpeechRecognizer(SpeechRecognitionConfig config);
    }
}
