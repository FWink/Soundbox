using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soundbox.Audio.Concentus
{
    /// <summary>
    /// Reads an <see cref="StreamAudioFormatType.Opus"/> audio track from an
    /// <see cref="ContainerFormatType.Ogg"/>, <see cref="ContainerFormatType.Mkv"/> or <see cref="ContainerFormatType.Webm"/> file
    /// and provides the decoded audio.
    /// </summary>
    public class ConcentusOggOpusStreamAudioSource : IStreamAudioSource
    {
        protected const int OutputSampleRate = 48000;
        protected const int OutputChannels = 1;

        public WaveStreamAudioFormat Format { get; }

        protected AudioBlob AudioInput;

        /// <summary>
        /// Used to extract an Ogg/Opus stream from an Mkv or webm stream.
        /// </summary>
        protected Matroska.MatroskaOpusReader MkvReader;

        /// <summary>
        /// Used to stop the task started in <see cref="Start"/>.
        /// </summary>
        protected CancellationTokenSource Cancellation;

        protected IServiceProvider ServiceProvider;

        public ConcentusOggOpusStreamAudioSource(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            this.Format = WaveStreamAudioFormat.GetIntFormat(OutputSampleRate, 16, OutputChannels, signed: true, littleEndian: BitConverter.IsLittleEndian);
        }

        /// <summary>
        /// Sets this stream's input source before <see cref="Start"/> is called.
        /// Throws an <see cref="ArgumentException"/> when the audio format is not supported.
        /// </summary>
        /// <param name="audioInput"></param>
        public void SetInput(AudioBlob audioInput)
        {
            if (audioInput.Format is ContaineredStreamAudioFormat containered)
            {
                if (containered.ContainerFormat == ContainerFormatType.Mkv || containered.ContainerFormat == ContainerFormatType.Webm)
                {
                    //need to convert first
                    var mkvReader = ServiceProvider.GetService(typeof(Matroska.MatroskaOpusReader)) as Matroska.MatroskaOpusReader;
                    if (mkvReader == null)
                    {
                        throw new ArgumentException($"AudioBlob is of container type {containered.ContainerFormat} but no Matroska reader is available");
                    }
                    this.MkvReader = mkvReader;
                }
                else if (containered.ContainerFormat != ContainerFormatType.Ogg)
                {
                    throw new ArgumentException($"AudioBlob is of unexpected container type {containered.ContainerFormat}");
                }
            }
            else
            {
                throw new ArgumentException("AudioBlob has no container type but must be of type Ogg/Opus");
            }
            if (audioInput.Format.Type != StreamAudioFormatType.Opus)
            {
                throw new ArgumentException($"AudioBlob is of unexpected audio type {audioInput.Format.Type}");
            }

            this.AudioInput = audioInput;
        }

        /// <summary>
        /// Returns true if a stream of this format is supported by this reader.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static bool IsSupported(StreamAudioFormat format)
        {
            return format is ContaineredStreamAudioFormat containered &&
                (containered.ContainerFormat == ContainerFormatType.Ogg || containered.ContainerFormat == ContainerFormatType.Webm || containered.ContainerFormat == ContainerFormatType.Mkv) &&
                containered.AudioFormat.Type == StreamAudioFormatType.Opus;
        }

        public Task Start()
        {
            Cancellation?.Dispose();
            Cancellation = new CancellationTokenSource();

            var token = Cancellation.Token;
            //start a worker: we may need to unwrap an MKV stream (synchronously)
            return Task.Run(() =>
            {
                AudioBlob audio = AudioInput;

                if (MkvReader != null)
                {
                    //convert first
                    audio = MkvReader.ReadOggOpus(audio);
                }

                if (token.IsCancellationRequested)
                {
                    audio.Dispose();
                    token.ThrowIfCancellationRequested();
                }

                //start decoding on yet another thread (so that the Start task completes)
                Threading.Tasks.FireAndForget(() =>
                {
                    try
                    {
                        var decoder = new global::Concentus.Structs.OpusDecoder(OutputSampleRate, OutputChannels);
                        var reader = new global::Concentus.Oggfile.OpusOggReadStream(decoder, audio.Stream);

                        byte[] buffer = new byte[19200];
                        while (!Cancellation.IsCancellationRequested)
                        {
                            short[] samples = reader.DecodeNextPacket();
                            if (samples == null)
                            {
                                //end of stream
                                Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                                {
                                    Cause = StreamAudioSourceStoppedCause.End
                                });
                                return;
                            }

                            //convert from short samples to byte array and raise output events
                            //if need be, raise multiple events per packet if samples > buffer
                            int samplesPerEvent = Math.Min(samples.Length, buffer.Length / sizeof(short));

                            for (int sampleOffset = 0; sampleOffset < samples.Length; sampleOffset += samplesPerEvent)
                            {
                                int sampleStopEvent = Math.Min(samples.Length, sampleOffset + samplesPerEvent);
                                int sampleCount = sampleStopEvent - sampleOffset;

                                for (int iSample = 0; iSample < sampleCount; ++iSample)
                                {
                                    Span<byte> convertTarget = new Span<byte>(buffer, iSample * sizeof(short), sizeof(short));
                                    BitConverter.TryWriteBytes(convertTarget, samples[sampleOffset + iSample]);
                                }

                                //raise event
                                DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                                {
                                    Format = this.Format,
                                    Buffer = new ArraySegment<byte>(buffer, 0, sampleCount * sizeof(short))
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                        {
                            Cause = StreamAudioSourceStoppedCause.Exception,
                            Exception = ex
                        });
                        throw;
                    }
                    finally
                    {
                        audio.Dispose();
                    }

                    Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                    {
                        Cause = StreamAudioSourceStoppedCause.Stopped
                    });
                });
            }, token);
        }

        public Task Stop()
        {
            Cancellation?.Cancel();
            return Task.CompletedTask;
        }

        public event EventHandler<StreamAudioSourceDataEvent> DataAvailable;
        public event EventHandler<StreamAudioSourceStoppedEvent> Stopped;

        public void Dispose()
        {
            AudioInput?.Dispose();
        }
    }
}
