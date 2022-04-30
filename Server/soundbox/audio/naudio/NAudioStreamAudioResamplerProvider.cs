using Microsoft.Extensions.Logging;
using Soundbox.Audio.Processing;
using System;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Factory for resamplers using the NAudio library
    /// </summary>
    public class NAudioStreamAudioResamplerProvider : IStreamAudioResamplerProvider
    {
        protected IServiceProvider ServiceProvider;
        protected ILogger Logger;

        public NAudioStreamAudioResamplerProvider(IServiceProvider serviceProvider, ILogger<NAudioStreamAudioResamplerProvider> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        public IStreamAudioResampler GetResampler(IStreamAudioSource input, WaveStreamAudioFormat outputFormat)
        {
            //try MediaFoundation first (Windows)
            var mediaFoundationResampler = ServiceProvider.GetService(typeof(NAudioMediaFoundationStreamAudioResampler)) as NAudioMediaFoundationStreamAudioResampler;
            if (mediaFoundationResampler != null)
            {
                try
                {
                    mediaFoundationResampler.Initialize(input, outputFormat);
                    return mediaFoundationResampler;
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not initialize NAudio MediaFoundation resampler");
                    mediaFoundationResampler.Dispose();
                }
            }

            return null;
        }
    }
}
