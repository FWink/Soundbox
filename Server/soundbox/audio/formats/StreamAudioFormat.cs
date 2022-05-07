namespace Soundbox.Audio
{
    /// <summary>
    /// Represents the format of a <see cref="IStreamAudioSink"/> or <see cref="IStreamAudioSource"/>: it defines
    /// the byte structure returned by sinks or written into soures.<br/>
    /// Depending on <see cref="Type"/>, specialized classes such as <see cref="WaveStreamAudioFormat"/> may provide additional information on the exact format.
    /// </summary>
    public class StreamAudioFormat
    {
        public virtual StreamAudioFormatType Type { get; protected set; }

        public StreamAudioFormat(StreamAudioFormatType type)
        {
            Type = type;
        }
    }
}
