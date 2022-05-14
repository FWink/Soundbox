namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionMatchResult
    {
        public ISpeechRecognizable Recognizable { get; set; }

        /// <summary>
        /// True: the recognizable matches the spoken text exactly or close enough.
        /// </summary>
        public bool Success { get; set; }
    }
}
