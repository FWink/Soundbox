namespace Soundbox.Audio
{
    public enum StreamAudioFormatType
    {
        Unknown = 0,
        /// <summary>
        /// See <see cref="WaveStreamAudioFormat"/>
        /// </summary>
        Wave,
        Mp3,
        Vorbis,
        Opus,
        Aac
    }
}
