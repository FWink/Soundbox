using Microsoft.Extensions.Logging;
using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Uses all installed libraries to try and find an <see cref="IStreamAudioSource"/> for <see cref="AudioBlob"/>s.
    /// </summary>
    public class DefaultBlobStreamAudioSourceProvider : IBlobStreamAudioSourceProvider
    {
        protected IServiceProvider ServiceProvider;
        protected ILogger Logger;

        public DefaultBlobStreamAudioSourceProvider(IServiceProvider serviceProvider, ILogger<DefaultBlobStreamAudioSourceProvider> logger)
        {
            this.ServiceProvider = serviceProvider;
            this.Logger = logger;
        }

        public IStreamAudioSource GetStreamAudioSource(AudioBlob blob)
        {
            //check if this is an Opus stream
            if (Concentus.ConcentusOggOpusStreamAudioSource.IsSupported(blob.Format))
            {
                var concentusSource = ServiceProvider.GetService(typeof(Concentus.ConcentusOggOpusStreamAudioSource)) as Concentus.ConcentusOggOpusStreamAudioSource;
                try
                {
                    if (concentusSource != null)
                    {
                        concentusSource.SetInput(blob);
                        return concentusSource;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Could not open a Concentus OggOpus reader");
                    concentusSource?.Dispose();
                }
            }

            //try NAudio as a general fallback
            var naudioProvider = ServiceProvider.GetService(typeof(NAudio.NAudioBlobStreamAudioSourceProvider)) as IBlobStreamAudioSourceProvider;
            if (naudioProvider != null)
            {
                var naudioSource = naudioProvider.GetStreamAudioSource(blob);
                if (naudioSource != null)
                {
                    return naudioSource;
                }
            }

            return null;
        }
    }
}
