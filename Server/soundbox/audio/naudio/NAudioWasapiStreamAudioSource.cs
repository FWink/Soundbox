using NAudio.CoreAudioApi;
using NAudio.Wave;
using Nito.AsyncEx;
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
        /// True: recording is currently running.
        /// </summary>
        protected bool Running;

        /// <summary>
        /// True: <see cref="WasapiCapture.RecordingStopped"/> is raised because <see cref="Stop"/> has been called.
        /// </summary>
        protected bool StopRequested;

        /// <summary>
        /// Apparently, <see cref="WasapiCapture.StartRecording"/> may fail when called immediately after <see cref="WasapiCapture.StopRecording"/>.
        /// This here is used to wait for the event <see cref="WasapiCapture.RecordingStopped"/> in <see cref="Stop"/>:
        /// <see cref="Stop"/> will return only when our capture is actually stopped and ready to start again.
        /// </summary>
        protected readonly AsyncMonitor StopCompleted = new AsyncMonitor();

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

            this.Capture.RecordingStopped += async (s, e) =>
            {
                Running = false;

                using (await StopCompleted.EnterAsync())
                {
                    StopCompleted.PulseAll();
                }

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
                Running = true;
            });
        }

        public async Task Stop()
        {
            Task stopCompleted = WaitForStopCompleted();

            StopRequested = true;
            Capture.StopRecording();

            //wait until fully stopped
            await stopCompleted;

            async Task WaitForStopCompleted()
            {
                if (!Running)
                    return;
                //wait for our condition variable
                using (await StopCompleted.EnterAsync())
                {
                    await StopCompleted.WaitAsync();
                }
            }
        }

        public event EventHandler<StreamAudioSourceDataEvent> DataAvailable;
        public event EventHandler<StreamAudioSourceStoppedEvent> Stopped;

        public void Dispose()
        {
            Capture?.Dispose();
        }
    }
}
