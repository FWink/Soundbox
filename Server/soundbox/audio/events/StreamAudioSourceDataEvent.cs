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
        public byte[] Buffer { get; set; }

        /// <summary>
        /// Number of bytes available in <see cref="Buffer"/>
        /// </summary>
        public int BytesAvailable { get; set; }

        /// <summary>
        /// Format of the audio source.
        /// </summary>
        public WaveStreamAudioFormat Format { get; set; }
    }
}
