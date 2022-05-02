namespace Soundbox.AppSettings
{
    /// <summary>
    /// Global <see cref="Soundbox"/> settings. Retrieved via DI with <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>
    /// </summary>
    public class SoundboxAppSettings
    {
        /// <summary>
        /// Speech recognition options for the soundbox's voice chat monitoring.
        /// </summary>
        public SoundboxSpeechRecognitionAppSettings SpeechRecognition { get; set; }
    }
}
