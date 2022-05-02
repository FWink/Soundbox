using System;

namespace Soundbox.Audio.Processing.Noisegate
{
    /// <summary>
    /// Options supplied to <see cref="INoiseGateStreamAudioProcessor"/> to control its behavior.
    /// </summary>
    public class NoiseGateStreamAudioProcessorOptions
    {
        /// <summary>
        /// Volume on a scale of of [0;100]. This is mapped into the sample range of the input format
        /// (e.g., for signed 16bit int encoding, this is mapped to [0;-32768] or [0;32767]).<br/>
        /// Audio samples equals or smaller than this threshold are dropped/caught/....
        /// I.e., using 0 drops only absolute 0 values, using 100 drops everything but distorted floats (amplitude is greater than 1).
        /// </summary>
        public float VolumeThreshold { get; set; } = -1;

        /// <summary>
        /// True: when the volume drops below the threshold, <see cref="IStreamAudioSource.DataAvailable"/> events stop being raised until the volume increases again.<br/>
        /// False: when the volume drops below the threshold, the amplitude of the returned samples is decreased to 0 (i.e., all samples returned will be 0).
        /// </summary>
        public bool ModeCutOff { get; set; } = true;

        /// <summary>
        /// The volume needs to drop below <see cref="VolumeThreshold"/> for at least this time before the noise gate kicks in.<br/>
        /// The absolute minimum time here is the time that the consumer takes to "settle down" into idle state.
        /// Especially important in <see cref="ModeCutOff"/>: consumers may be stuck waiting for data, so this is the time
        /// after which you'd expect the consumer to have processed all available data and when they'll be fine with not receiving further samples for a while.<br/>
        /// To be on the safe side, you'd probably want to use a higher value than the absolute minimum here and then (if implemented) use the absolute minimum for <see cref="DelayStopDetection"/>.
        /// </summary>
        public TimeSpan Delay { get; set; }

        /// <summary>
        /// Shorter <see cref="Delay"/> that is used when <see cref="INoiseGateStreamAudioProcessor.OnAudioStop"/> is called:
        /// this is either the typical time that the consumer takes to detect that the volume level dropped below the threshold
        /// (e.g., the typical time that a speech recognizer takes to detect that no one is speaking anymore);
        /// or the absolute minimum time mentioned in <see cref="Delay"/>.
        /// </summary>
        public TimeSpan DelayStopDetection { get; set; } = TimeSpan.MinValue;
    }
}
