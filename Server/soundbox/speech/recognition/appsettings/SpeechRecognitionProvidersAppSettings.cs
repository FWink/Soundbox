namespace Soundbox.Speech.Recognition.AppSettings
{
    /// <summary>
    /// Contains provider specific app settings for each implemented speech-to-text service.
    /// </summary>
    public class SpeechRecognitionProvidersAppSettings
    {
        /// <summary>
        /// Settings for <see cref="global::Soundbox.Speech.Recognition.Azure.AzureSpeechRecognitionService"/>
        /// </summary>
        public Azure.AzureSpeechRecognitionProviderAppSettings Azure { get; set; }
    }
}
