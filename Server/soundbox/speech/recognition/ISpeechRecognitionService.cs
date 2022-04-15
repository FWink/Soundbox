using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Speech-to-text service that listens to (live) audio on a given audio source and
    /// extracts the spoken words in near realtime.<br/>
    /// Use <see cref="ISpeechRecognitionServiceProvider"/> to get an instance of this class.
    /// </summary>
    public interface ISpeechRecognitionService : IDisposable
    {
        /// <summary>
        /// Starts listening to the audio source and starts transcribing words.<br/>
        /// Returns a task that resolves once the recognition has started (e.g., once connection to a cloud service has been established).
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Task Start(SpeechRecognitionOptions options);

        /// <summary>
        /// Immediately stops the recognition started via <see cref="Start(SpeechRecognitionOptions)"/>.
        /// </summary>
        /// <returns></returns>
        Task Stop();

        /// <summary>
        /// Updates an ongoing recognition (if any) with the given options.
        /// Likely, this will simply cause <see cref="Stop"/> and <see cref="Start(SpeechRecognitionOptions)"/> to be called.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        Task UpdateOptions(SpeechRecognitionOptions options);

        /// <summary>
        /// Event that is triggered whenever words have been recognized in the audio stream.
        /// </summary>
        event EventHandler<SpeechRecognizedEvent> Recognized;

        /// <summary>
        /// Triggered when the speech recognition is permanently stopped for whatever reason (like calling <see cref="Stop"/>).
        /// Simply calling <see cref="Start(SpeechRecognitionOptions)"/> again might resolve the issue.
        /// </summary>
        event EventHandler<SpeechRecognitionStoppedEvent> Stopped;
    }
}
