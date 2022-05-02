using System.Collections.Generic;

namespace Soundbox.Speech.Recognition.AppSettings.Azure
{
    public class AzureSpeechRecognitionProviderAppSettings
    {
        /// <summary>
        /// List of credentials/speech services used. This must be 1 at least and usually 2 at most:
        /// when the quota limit on one service has been reached, the speech recognizer cycles through this list and continues using the next service.
        /// This way, you can use a free service for a limited time per month and then it will switch to a pay-as-you-go service automatically.
        /// </summary>
        public IList<AzureSpeechRecognitionCredentials> Credentials { get; set; }
    }
}
