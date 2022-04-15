using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Triggered when the speech recognition stops for whatever reason.
    /// </summary>
    public class SpeechRecognitionStoppedEvent : SpeechRecognitionBaseEvent
    {
        /// <summary>
        /// Exception that caused the recognition to stop.
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Message that describes why the recognition stopped (for logging mostly).
        /// </summary>
        public string Message { get; set; }
    }
}
