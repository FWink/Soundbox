using NAudio.Wave;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Converts NAudio's <see cref="WaveStream"/> into our internal <see cref="IStreamAudioSource"/>.
    /// Since our <see cref="IStreamAudioSource"/> uses a push-based approach but <see cref="WaveStream"/> uses pull,
    /// we start a background task to continuously read from the source stream.
    /// </summary>
    public class NAudioWaveStreamToStreamAudioSourceAdapter : IStreamAudioSource
    {
        public WaveStreamAudioFormat Format { get; private set; }

        public WaveStream SourceStream { get; private set; }

        /// <summary>
        /// Used to stop the task started in <see cref="Start"/>.
        /// </summary>
        protected CancellationTokenSource Cancellation;

        public NAudioWaveStreamToStreamAudioSourceAdapter(WaveStream stream)
        {
            this.SourceStream = stream;
            this.Format = NAudioUtilities.FromNAudioWaveFormat(stream.WaveFormat);
        }

        public Task Start()
        {
            Cancellation?.Dispose();
            Cancellation = new CancellationTokenSource();

            var token = Cancellation.Token;
            //start a background task
            Threading.Tasks.FireAndForget(async () =>
            {
                try
                {
                    byte[] buffer = new byte[19200];
                    while (!token.IsCancellationRequested)
                    {
                        int read = await SourceStream.ReadAsync(buffer, 0, buffer.Length, token);
                        if (read == 0)
                        {
                            //end of stream reached
                            Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                            {
                                Message = "End of stream"
                            });
                            return;
                        }
                        DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                        {
                            Buffer = new ArraySegment<byte>(buffer, 0, read),
                            Format = Format
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                    {
                        Message = "Stopped"
                    });
                }
                catch (Exception ex)
                {
                    Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                    {
                        Exception = ex
                    });
                    throw;
                }
            });

            return Task.CompletedTask;
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
            Cancellation?.Cancel();
            Cancellation?.Dispose();
            Cancellation = null;
            SourceStream.Dispose();
        }
    }
}
