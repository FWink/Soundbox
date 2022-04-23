﻿using NAudio.CoreAudioApi;
using Soundbox.Threading;
using System;
using System.Threading.Tasks;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Uses the NAudio+WASAPI library to read from audio devices.
    /// </summary>
    public class NAudioWasapiStreamAudioSource : IStreamAudioSource
    {
        public AudioDevice Device { get; private set; }

        public WaveStreamAudioFormat Format { get; private set; }

        protected WasapiCapture Capture;

        /// <summary>
        /// Initializes this audio source with the given audio device. <see cref="Start"/> may be called afterwards.
        /// </summary>
        /// <param name="device"></param>
        public void SetAudioDevice(AudioDevice device)
        {
            this.Device = device;

            var naudioDevice = NAudioUtilities.GetDevice(device);
            this.Capture = new WasapiCapture(naudioDevice);
            this.Format = NAudioUtilities.FromNAudioWaveFormat(this.Capture.WaveFormat);

            //set up event listeners
            this.Capture.DataAvailable += (s, e) =>
            {
                this.DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                {
                    Buffer = e.Buffer,
                    BytesAvailable = e.BytesRecorded,
                    Format = Format
                });
            };

            this.Capture.RecordingStopped += (s, e) =>
            {
                this.Stopped?.Invoke(this, new StreamAudioSourceStoppedEvent()
                {
                    Exception = e.Exception,
                    Message = e.Exception == null ? "Unknown" : e.Exception.Message
                });
            };
        }

        public Task Start()
        {
            return Tasks.Taskify(() =>
            {
                Capture.StartRecording();
            });
        }

        public Task Stop()
        {
            return Tasks.Taskify(() =>
            {
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
