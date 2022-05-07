using NAudio.CoreAudioApi;
using NAudio.Wave;
using Soundbox.Threading;
using System;
using System.Threading.Tasks;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Uses the NAudio+WASAPI library to read from audio devices on Windows.
    /// </summary>
    public class NAudioWasapiStreamAudioSource : IStreamAudioSource
    {
        public AudioDevice Device { get; private set; }

        public WaveStreamAudioFormat Format { get; private set; }

        protected WasapiCapture Capture;

        /// <summary>
        /// True: <see cref="WasapiCapture.RecordingStopped"/> is raised because <see cref="Stop"/> has been called.
        /// </summary>
        protected bool StopRequested;

        /// <summary>
        /// Initializes this audio source with the given audio device. <see cref="Start"/> may be called afterwards.
        /// </summary>
        /// <param name="device"></param>
        public void SetAudioDevice(AudioDevice device)
        {
            this.Device = device;

            var naudioDevice = NAudioUtilities.GetDevice(device);
            if (naudioDevice.DataFlow == DataFlow.Capture)
                this.Capture = new WasapiCapture(naudioDevice);
            else
                this.Capture = new WasapiLoopbackCapture(naudioDevice);
            this.Format = NAudioUtilities.FromNAudioWaveFormat(this.Capture.WaveFormat);

            //set up event listeners
            this.Capture.DataAvailable += (s, e) =>
            {
                if (e.BytesRecorded == 0)
                    return;

                this.DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                {
                    Buffer = new ArraySegment<byte>(e.Buffer, 0, e.BytesRecorded),
                    Format = Format
                });
            };

            this.Capture.RecordingStopped += (s, e) =>
            {
                var cause = StreamAudioSourceStoppedCause.Unknown;
                if (StopRequested)
                    cause = StreamAudioSourceStoppedCause.Stopped;
                else if (e.Exception != null)
                    cause = StreamAudioSourceStoppedCause.Exception;

                this.Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                {
                    Cause = cause,
                    Exception = e.Exception
                });
            };
        }

        public Task Start()
        {
            return Tasks.Taskify(() =>
            {
                StopRequested = false;
                Capture.StartRecording();
            });
        }

        public Task Stop()
        {
            return Tasks.Taskify(() =>
            {
                StopRequested = true;
                Capture.StopRecording();
            });
        }

        public event EventHandler<StreamAudioSourceDataEvent> DataAvailable;
        public event EventHandler<StreamAudioSourceStoppedEvent> Stopped;

        public void Dispose()
        {
            Capture?.Dispose();
        }
    }
}
