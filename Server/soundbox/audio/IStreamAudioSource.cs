using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Audio
{
    /// <summary>
    /// Audio source that provides uncompressed audio as a stream of bytes in some specified format (PCM with parameters for sample rate, bit depth etc)<br/>
    /// Doesn't actually do anything yet and is here only as a placeholder.
    /// </summary>
    public interface IStreamAudioSource : IAudioSource
    {
        /// <summary>
        /// Returns the stream's audio format.
        /// </summary>
        WaveStreamAudioFormat Format { get; }

        /// <summary>
        /// Starts recording, thus causing <see cref="DataAvailable"/> to start raising events.
        /// </summary>
        /// <returns>
        /// Task completes once recording has started (e.g., after initializing the hardware has finished).
        /// </returns>
        Task Start();

        /// <summary>
        /// Stops recording, thus causing <see cref="DataAvailable"/> to be stop raising events (though buffered samples may cause a final event).
        /// </summary>
        /// <returns>
        /// Task completes once recording has stopped (e.g., after the hardware has been shut down).
        /// </returns>
        Task Stop();

        /// <summary>
        /// Raised when samples have been read from an audio device or other kind of audio source and they are now ready to retrieve.
        /// </summary>
        event EventHandler<StreamAudioSourceDataEvent> DataAvailable;

        /// <summary>
        /// Raised when recording has stopped (either because <see cref="Stop"/> has been called or because some error occurred).
        /// </summary>
        event EventHandler<StreamAudioSourceStoppedEvent> Stopped;
    }
}
