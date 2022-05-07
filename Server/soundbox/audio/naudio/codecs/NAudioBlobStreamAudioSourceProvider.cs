using Microsoft.Extensions.Logging;
using NAudio.Wave;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Uses the NAudio library to decode blob audio streams of these formats:<list type="bullet">
    /// <item><see cref="StreamAudioFormatType.Wave"/></item>
    /// <item><see cref="StreamAudioFormatType.Vorbis"/></item>
    /// </list>
    /// </summary>
    public class NAudioBlobStreamAudioSourceProvider : IBlobStreamAudioSourceProvider
    {
        protected ILogger Logger;

        public NAudioBlobStreamAudioSourceProvider(ILogger<NAudioBlobStreamAudioSourceProvider> logger)
        {
            this.Logger = logger;
        }

        public IStreamAudioSource GetStreamAudioSource(AudioBlob blob)
        {
            if (blob.Stream == null)
            {
                Logger.LogInformation("NAudioBlobStreamAudioSourceProvider can read from streams only (is null)");
                return null;
            }

            WaveStream waveStream = null;

            switch (blob.Format.Type)
            {
                case StreamAudioFormatType.Wave:
                    waveStream = new WaveFileReader(blob.Stream);
                    break;
                case StreamAudioFormatType.Vorbis:
                    waveStream = new global::NAudio.Vorbis.VorbisWaveReader(blob.Stream);
                    break;
            }

            if (waveStream == null)
                return null;
            return new BlobStreamAudioWrapper(new NAudioWaveStreamToStreamAudioSourceAdapter(waveStream), blob);
        }

        /// <summary>
        /// Wrapper around our result streams that disposes both the audio stream and the source blob.
        /// </summary>
        protected class BlobStreamAudioWrapper : Processing.IWrappedStreamAudioSource
        {
            public IStreamAudioSource WrappedAudioSource { get; private set; }

            public AudioBlob Blob { get; private set; }

            public BlobStreamAudioWrapper(IStreamAudioSource wrappedAudioSource, AudioBlob blob)
            {
                WrappedAudioSource = wrappedAudioSource;
                Blob = blob;
            }

            public void Dispose()
            {
                WrappedAudioSource.Dispose();
                Blob.Dispose();
            }
        }
    }
}
