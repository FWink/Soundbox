using System.Collections.Generic;

namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Represents a voice-activated "command": one or more text triggers (words or event entire sentences)
    /// that can be matched against the result of a speech recognition operation (<see cref="SpeechRecognizedEvent"/>).
    /// </summary>
    public interface ISpeechRecognizable
    {
        /// <summary>
        /// Words or sentences that trigger this voice-activated command.
        /// </summary>
        ICollection<string> SpeechTriggers { get; }
    }
}
