using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundbox.Speech.Recognition.AppSettings;
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

        protected SpeechRecognitionAppSettings AppSettings;

        public AzureSpeechRecognitionServiceProvider(IServiceProvider serviceProvider, ILogger<AzureSpeechRecognitionServiceProvider> logger, IOptions<SpeechRecognitionAppSettings> appSettings)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
            this.AppSettings = appSettings.Value;
        }

        public ISpeechRecognitionService GetSpeechRecognizer(SpeechRecognitionConfig config)
        {
            //check on azure settings and credentials
            var azureSettings = AppSettings?.Providers?.Azure;
            if (azureSettings == null || azureSettings.Credentials == null || azureSettings.Credentials.Count == 0)
                //not set up
                return null;
            foreach (var credentials in azureSettings.Credentials)
            {
                if (credentials == null || string.IsNullOrWhiteSpace(credentials.Region) || string.IsNullOrWhiteSpace(credentials.SubscriptionKey))
                {
                    Logger.LogWarning("No or invalid Azure Speech-to-text credentials");
                    return null;
                }
            }

            var service = ServiceProvider.GetService(typeof(AzureSpeechRecognitionService)) as AzureSpeechRecognitionService;
            if (service == null)
                return null;

            try
            {
                if (service.SetConfig(config, AppSettings))
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
