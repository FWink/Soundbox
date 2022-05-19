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
                this.Format = new StreamAudioFormat(StreamAudioFormatType.Unknown);

                if (mimeType == "audio/wav" ||
                    mimeType == "audio/x-wav" ||
                    mimeType == "audio/wave")
                {
                    this.Format = new StreamAudioFormat(StreamAudioFormatType.Wave);
                }
                else if (mimeType == "audio/mpeg" ||
                    mimeType == "audio/mp3")
                {
                    this.Format = new StreamAudioFormat(StreamAudioFormatType.Mp3);
                }
                else if (mimeType == "audio/opus")
                {
                    this.Format = new StreamAudioFormat(StreamAudioFormatType.Opus);
                }
                else if (mimeType == "audio/vorbis")
                {
                    this.Format = new StreamAudioFormat(StreamAudioFormatType.Vorbis);
                }
                else if (mimeType == "audio/aac")
                {
                    this.Format = new StreamAudioFormat(StreamAudioFormatType.Aac);
                }
                else if (mimeType.StartsWith("audio/ogg"))
                {
                    //ogg container format
                    if (mimeType.Contains("codecs=vorbis"))
                    {
                        this.Format = new ContaineredStreamAudioFormat(ContainerFormatType.Ogg, new StreamAudioFormat(StreamAudioFormatType.Vorbis));
                    }
                    else
                    {
                        //assume opus
                        this.Format = new ContaineredStreamAudioFormat(ContainerFormatType.Ogg, new StreamAudioFormat(StreamAudioFormatType.Opus));
                    }
                }
                else if (mimeType.StartsWith("audio/webm"))
                {
                    //webm container format
                    if (mimeType.Contains("codecs=opus"))
                    {
                        this.Format = new ContaineredStreamAudioFormat(ContainerFormatType.Webm, new StreamAudioFormat(StreamAudioFormatType.Opus));
                    }
                    else if (mimeType.Contains("codecs=vorbis"))
                    {
                        this.Format = new ContaineredStreamAudioFormat(ContainerFormatType.Webm, new StreamAudioFormat(StreamAudioFormatType.Vorbis));
                    }
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
