using System.IO;

namespace Soundbox.Audio.Matroska
{
    /// <summary>
    /// Reads Opus audio tracks from matroska files: <see cref="ContainerFormatType.Mkv"/> and <see cref="ContainerFormatType.Webm"/>
    /// </summary>
    public class MatroskaOpusReader
    {
        /// <summary>
        /// Synchronously reads the Opus audio stream from the given audio blob and wraps it in an Ogg container.
        /// The resulting blob has the <see cref="ContainerFormatType.Ogg"/> container format and <see cref="StreamAudioFormatType.Opus"/> audio format.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public AudioBlob ReadOggOpus(AudioBlob input)
        {
            var output = new MemoryStream();

            global::Matroska.Muxer.MatroskaDemuxer.ExtractOggOpusAudio(input.Stream, output);

            output.Position = 0;
            return AudioBlob.FromStream(output, new ContaineredStreamAudioFormat(ContainerFormatType.Ogg, new StreamAudioFormat(StreamAudioFormatType.Opus)));
        }
    }
}
