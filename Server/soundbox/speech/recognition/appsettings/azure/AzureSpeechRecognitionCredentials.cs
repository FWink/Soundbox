namespace Soundbox.Speech.Recognition.AppSettings.Azure
{
    /// <summary>
    /// One set of credentials which represents one unique speech-to-text service ("Speech service" in Azure portal).<br/>
    /// Settings here can be retrieved under "Speech service" -> "Keys and Endpoints"
    /// </summary>
    public class AzureSpeechRecognitionCredentials
    {
        /// <summary>
        /// E.g., "westeurope"
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Key to authenticate with the Azure speech-to-text service.
        /// </summary>
        public string SubscriptionKey { get; set; }
    }
}
