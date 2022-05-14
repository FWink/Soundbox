using System.Collections.Generic;

namespace Soundbox.Speech.Recognition
{
    public class SpeechRecognitionTestRecognizable : ISpeechRecognizable
    {
        /// <summary>
        /// Arbitrary ID that the client can use to identify the recognizable that has been detected.
        /// </summary>
        public string ID { get; set; }

        public ICollection<string> SpeechTriggers { get; set; }
    }
}
