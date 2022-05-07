using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Uses the default installed <see cref="IDeviceStreamAudioSourceProvider"/> and <see cref="IBlobStreamAudioSourceProvider"/>.
    /// </summary>
    public class DefaultStreamAudioSourceProvider : IStreamAudioSourceProvider
    {
        protected IServiceProvider ServiceProvider;

        public DefaultStreamAudioSourceProvider(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public IStreamAudioSource GetStreamAudioSource(IAudioSource source)
        {
            IStreamAudioSource streamSource = source as IStreamAudioSource;
            if (streamSource != null)
                return streamSource;

            if (source is AudioDevice audioDevice)
            {
                var provider = ServiceProvider.GetService(typeof(IDeviceStreamAudioSourceProvider)) as IDeviceStreamAudioSourceProvider;
                streamSource = provider?.GetStreamAudioSource(audioDevice);
                if (streamSource != null)
                    return streamSource;
            }

            if (source is AudioBlob audioBlob)
            {
                var provider = ServiceProvider.GetService(typeof(IBlobStreamAudioSourceProvider)) as IBlobStreamAudioSourceProvider;
                streamSource = provider?.GetStreamAudioSource(audioBlob);
                if (streamSource != null)
                    return streamSource;
            }

            return null;
        }
    }
}
