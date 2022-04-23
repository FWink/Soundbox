using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Raised when a <see cref="IStreamAudioSource"/> stopped recording for some reason.
    /// </summary>
    public class StreamAudioSourceStoppedEvent : StreamAudioEvent
    {
        /// <summary>
        /// Exception that caused the stream to stop.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Message that describes why the stream stopped (for logging mostly).
        /// </summary>
        public string Message { get; set; }
    }
}
