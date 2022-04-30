using Microsoft.Extensions.Logging;
using System;

namespace Soundbox.Audio.Processing.Noisegate.Implementation
{
    /// <summary>
    /// Factory for <see cref="DefaultNoiseGateStreamAudioProcessor"/>
    /// </summary>
    public class DefaultNoiseGateStreamAudioProcessProvider : INoiseGateStreamAudioProcessProvider
    {
        protected IServiceProvider ServiceProvider;
        protected ILogger Logger;

        public DefaultNoiseGateStreamAudioProcessProvider(IServiceProvider serviceProvider, ILogger<DefaultNoiseGateStreamAudioProcessProvider> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        public INoiseGateStreamAudioProcessor GetNoiseGate(IStreamAudioSource audioSource)
        {
            //check the input format
            var format = audioSource.Format;
            if (format.FloatEncoded)
            {
                if (format.BitsPerSample != 32)
                {
                    Logger.LogInformation($"Unsupported float size: {format.BitsPerSample}");
                    return null;
                }
            }
            else if (format.IntEncoded)
            {
                if (format.BitsPerSample > 32 || (format.BitsPerSample % 8) != 0)
                {
                    Logger.LogInformation($"Unsupported int size: {format.BitsPerSample}");
                    return null;
                }
            }
            else
            {
                Logger.LogInformation($"Unsupported wave enconding: neither float nor int");
                return null;
            }

            var noiseGate = ServiceProvider.GetService(typeof(DefaultNoiseGateStreamAudioProcessor)) as DefaultNoiseGateStreamAudioProcessor;
            try
            {
                noiseGate?.SetAudioSource(audioSource);
                return noiseGate;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not initialize default noise gate");
                ((IDisposable) noiseGate)?.Dispose();
                return null;
            }
        }
    }
}
