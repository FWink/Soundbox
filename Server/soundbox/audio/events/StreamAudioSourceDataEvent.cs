using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Event raised by <see cref="IStreamAudioSource"/> when samples are ready to be read.
    /// </summary>
    public class StreamAudioSourceDataEvent : StreamAudioEvent
    {
        /// <summary>
        /// Buffer from which the audio source's samples can be read.
        /// </summary>
        public ArraySegment<byte> Buffer { get; set; }

        /// <summary>
        /// Format of the audio source.
        /// </summary>
        public WaveStreamAudioFormat Format { get; set; }

        /// <summary>
        /// Returns <see cref="Buffer"/> segmented into array segments of size <see cref="WaveStreamAudioFormat.BitsPerSample"/>.
        /// </summary>
        public SampleCollection Samples => new SampleCollection(Format, Buffer);
    }
}
