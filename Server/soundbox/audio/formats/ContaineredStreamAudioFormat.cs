namespace Soundbox.Audio
{
    /// <summary>
    /// Audio format for audio streams wrapped in a container.
    /// Example: mp4 is a container format that may include (for example) video tracks encoded in h264 and audio encoded in AAC.<br/>
    /// We use a simplification: many container formats can have multiple audio tracks each with different encodings,
    /// but we model only one single audio track here. That should be good enough for our purposes.
    /// It's not all that common to have multiple audio tracks in the kind of files/streams that we're using here in the soundbox.
    /// </summary>
    public class ContaineredStreamAudioFormat : StreamAudioFormat
    {
        /// <summary>
        /// Format of the container
        /// </summary>
        public ContainerFormatType ContainerFormat { get; }

        /// <summary>
        /// Format of the included audio stream.
        /// </summary>
        public StreamAudioFormat AudioFormat { get; }

        public ContaineredStreamAudioFormat(ContainerFormatType containerFormat, StreamAudioFormat audioFormat) : base(audioFormat.Type)
        {
            this.ContainerFormat = containerFormat;
            this.AudioFormat = audioFormat;
        }
    }
}
