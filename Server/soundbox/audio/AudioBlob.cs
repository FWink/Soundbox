using System.IO;

namespace Soundbox.Audio
{
    /// <summary>
    /// An audio source that contains compressed or uncompressed audio data as a "blob":
    /// a generic data source such as a file or an HTTP stream.
    /// </summary>
    public class AudioBlob : IAudioSource
    {
        /// <summary>
        /// Raw data stream.
        /// </summary>
        public Stream Stream { get; private set; }

        /// <summary>
        /// Format of <see cref="Stream"/>. 
        /// </summary>
        public StreamAudioFormat Format { get; private set; }

        public string MimeType { get; private set; }

        protected AudioBlob(Stream stream, StreamAudioFormat format, string mimeType)
        {
            this.Stream = stream;
            this.MimeType = mimeType;

            if (format != null)
            {
                this.Format = format;
            }
            else
            {
                //try to guess from MimeType
                switch (mimeType)
                {
                    case "audio/wav":
                    case "audio/x-wav":
                    case "audio/wave":
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Wave);
                        break;
                    case "audio/mpeg":
                    case "audio/mp3":
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Mp3);
                        break;
                    case "audio/ogg":
                    case "audio/vorbis":
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Vorbis);
                        break;
                    case "audio/opus":
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Opus);
                        break;
                    case "audio/aac":
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Aac);
                        break;
                    default:
                        this.Format = new StreamAudioFormat(StreamAudioFormatType.Unknown);
                        break;
                }
            }
        }

        #region "Static Getters"

        /// <summary>
        /// Constructs an AudioBlob from the given stream. At least one of <paramref name="format"/> and <paramref name="mimeType"/> must be given.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="format"></param>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static AudioBlob FromStream(Stream stream, StreamAudioFormat format = null, string mimeType = null)
        {
            return new AudioBlob(stream, format, mimeType);
        }

        #endregion

        public void Dispose()
        {
            Stream?.Dispose();
        }
    }
}
