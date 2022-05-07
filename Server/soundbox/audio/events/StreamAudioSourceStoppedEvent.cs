using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Raised when a <see cref="IStreamAudioSource"/> stopped recording for some reason.
    /// </summary>
    public class StreamAudioSourceStoppedEvent : StreamAudioEvent
    {
        /// <summary>
        /// Cause why the stream stopped.
        /// </summary>
        public StreamAudioSourceStoppedCause Cause { get; set; }

        /// <summary>
        /// Exception that caused the stream to stop.
        /// </summary>
        public Exception Exception { get; set; }

        protected string _message;
        /// <summary>
        /// Message that describes why the stream stopped (for logging mostly).
        /// </summary>
        public string Message
        {
            get
            {
                if (_message != null)
                    return _message;
                switch (Cause)
                {
                    case StreamAudioSourceStoppedCause.Exception:
                        return "Exception: " + Exception.Message;
                    case StreamAudioSourceStoppedCause.Stopped:
                        return "Stopped (manually)";
                    case StreamAudioSourceStoppedCause.End:
                        return "End of stream";
                    default:
                        return Cause.ToString();
                }
            }
            set
            {
                _message = value;
            }
        }
    }
}
