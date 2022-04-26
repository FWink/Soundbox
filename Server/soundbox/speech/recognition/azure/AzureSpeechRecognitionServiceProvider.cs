using Microsoft.Extensions.Logging;
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
        protected ILogger Logger;

        public AzureSpeechRecognitionServiceProvider(IServiceProvider serviceProvider, ILogger<AzureSpeechRecognitionServiceProvider> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        public ISpeechRecognitionService GetSpeechRecognizer(SpeechRecognitionConfig config)
        {
            if (!(config.AudioSource is Audio.AudioDevice audioDevice) || (!audioDevice.UseDefaultAudioInputDevice && audioDevice.UseDefaultAudioOutputDevice))
                //not supported right now
                return null;

            var service = ServiceProvider.GetService(typeof(AzureSpeechRecognitionService)) as AzureSpeechRecognitionService;
            if (service == null)
                return null;

            try
            {
                if (service.SetConfig(config))
                    return service;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not initialize AzureSpeechRecognitionService");
                service.Dispose();
            }

            return null;
        }
    }
}
