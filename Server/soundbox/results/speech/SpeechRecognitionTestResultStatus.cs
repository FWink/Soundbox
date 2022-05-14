namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionTestResultStatus : ResultStatus
    {
        protected const ResultStatusSegment Segment = ResultStatusSegment.Volume;

        public static readonly SpeechRecognitionTestResultStatus INVALID_AUDIO_TYPE = new SpeechRecognitionTestResultStatus(3400, "The uploaded audio data type is not supported");
        public static readonly SpeechRecognitionTestResultStatus NOT_AVAILABLE = new SpeechRecognitionTestResultStatus(3500, "Speech recognition is not enabled on this server");

        private SpeechRecognitionTestResultStatus(int code, string message, bool success = false)
        {
            Code = code;
            Message = message;
            Success = success;
        }
    }
}
