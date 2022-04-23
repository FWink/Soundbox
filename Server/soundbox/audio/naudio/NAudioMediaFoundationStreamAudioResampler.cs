using NAudio.Wave;
using System;
using System.Threading.Tasks;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Uses NAudio's <see cref="global::NAudio.Wave.MediaFoundationResampler"/> to resample (Windows)
    /// </summary>
    public class NAudioMediaFoundationStreamAudioResampler : IStreamAudioResampler
    {
        public IStreamAudioSource WrappedAudioSource { get; private set; }

        public WaveStreamAudioFormat Format { get; private set; }


        protected NAudioStreamAudioSourceToWaveProviderAdapterSimple WaveProviderAdapter;
        protected MediaFoundationResampler Resampler;

        /// <summary>
        /// Output buffer for <see cref="Resampler"/>
        /// </summary>
        protected byte[] Buffer = new byte[38400];

        /// <summary>
        /// Initializes this resampler with the given input audio source and output format.
        /// Attaches to the given source's event to start resampling as soon as <see cref="IStreamAudioSource.DataAvailable"/> is raised.
        /// </summary>
        /// <param name="audioSource"></param>
        /// <param name="outputFormat"></param>
        public void Initialize(IStreamAudioSource audioSource, WaveStreamAudioFormat outputFormat)
        {
            this.WaveProviderAdapter = new NAudioStreamAudioSourceToWaveProviderAdapterSimple(audioSource);
            this.Resampler = new MediaFoundationResampler(this.WaveProviderAdapter, NAudioUtilities.ToNAudioWaveFormat(outputFormat));

            //set this *after* we initialize the resampler. if it throws, we won't dispose the input audio source by accident
            this.WrappedAudioSource = audioSource;
            this.Format = outputFormat;

            //handle events from the wrapped source
            audioSource.DataAvailable += (s, e) =>
            {
                //feed into our adapter
                WaveProviderAdapter.Write(e);

                //read from resampler and trigger our own output event
                int read;
                while ((read = Resampler.Read(Buffer, 0, Buffer.Length)) > 0)
                {
                    DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                    {
                        Buffer = Buffer,
                        BytesAvailable = read,
                        Format = Format
                    });
                }
            };
            audioSource.Stopped += (s, e) =>
            {
                Stopped?.Invoke(this, e);
            };
        }

        public void Dispose()
        {
            WrappedAudioSource?.Dispose();
            Resampler?.Dispose();
        }

        public event EventHandler<StreamAudioSourceDataEvent> DataAvailable;
        public event EventHandler<StreamAudioSourceStoppedEvent> Stopped;
    }
}
