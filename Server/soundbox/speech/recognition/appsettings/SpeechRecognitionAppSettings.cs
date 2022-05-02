namespace Soundbox.Speech.Recognition.AppSettings
{
    /// <summary>
    /// Static app settings for speech recognition. Sets up the possible providers with their API keys and further settings as required.
    /// </summary>
    public class SpeechRecognitionAppSettings
    {
        /// <summary>
        /// Set of implemented providers.
        /// </summary>
        public SpeechRecognitionProvidersAppSettings Providers { get; set; }
    }
}
