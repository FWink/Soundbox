namespace Soundbox.Speech.Recognition
{
    /// <summary>
    /// Result for a users's speech recognition test: user uploads a sound clip which we feed into a <see cref="ISpeechRecognitionService"/>.
    /// We then return the detected words along with information which (if any) of their spoken words matched their entered speech triggers.<br/>
    /// This is basically a wrapper around <see cref="SpeechRecognizedEvent"/> and <see cref="SpeechRecognitionMatchResult"/>.<br/>
    /// Multiple such results may be returned for one spoken clip: <see cref="SpeechEvent"/> will then only contain a partial result and will be updated live as the uploaded
    /// clip is being processed.
    /// </summary>
    public class SpeechRecognitionTestResult : ServerResult
    {
        /// <summary>
        /// Contains transcribed words that were spoken in the uploaded clip.
        /// </summary>
        public SpeechRecognizedEvent SpeechEvent { get; }

        /// <summary>
        /// Contains which words (if any) matched the input speech triggers. Included only if there was in fact a match for this event.
        /// </summary>
        public SpeechRecognitionMatchResult MatchResult { get; }

        /// <summary>
        /// True: this is the last event for the recorded clip. The clip has been processed completely.
        /// </summary>
        public bool End { get; }

        /// <summary>
        /// For errors
        /// </summary>
        /// <param name="status"></param>
        public SpeechRecognitionTestResult(ResultStatus status) : this(status, null, null, true) { }

        public SpeechRecognitionTestResult(ResultStatus status, SpeechRecognizedEvent speechEvent, SpeechRecognitionMatchResult matchResult, bool end) : base(status)
        {
            this.SpeechEvent = speechEvent;
            if (matchResult?.Success == true)
                this.MatchResult = matchResult;
            this.End = end;
        }
    }
}
