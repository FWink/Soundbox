namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionMatchResult
    {
        /// <summary>
        /// See <see cref="SpeechRecognizedEvent.Match(ISpeechRecognizable, SpeechRecognitionMatchState)"/>
        /// </summary>
        public SpeechRecognitionMatchState State { get; set; }

        public ISpeechRecognizable Recognizable { get; set; }

        /// <summary>
        /// True: the recognizable matches the spoken text exactly or close enough.
        /// </summary>
        public bool Success { get; set; }
    }
}
