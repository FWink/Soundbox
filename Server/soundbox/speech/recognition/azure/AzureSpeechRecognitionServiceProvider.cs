using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Speech.Recognition.Azure
{
    /// <summary>
    /// Provider for <see cref="AzureSpeechRecognitionService"/>
    /// </summary>
    public class AzureSpeechRecognitionServiceProvider : ISpeechRecognitionServiceProvider
    {
        protected IServiceProvider ServiceProvider;

        public AzureSpeechRecognitionServiceProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ISpeechRecognitionService GetSpeechRecognizer(SpeechRecognitionConfig config)
        {
            if (!(config.AudioSource is Audio.DeviceAudioSource))
                //not supported right now
                return null;

            var service = ServiceProvider.GetService(typeof(AzureSpeechRecognitionService)) as AzureSpeechRecognitionService;
            if (service == null)
                return null;

            service.SetConfig(config);
            return service;
        }
    }
}
