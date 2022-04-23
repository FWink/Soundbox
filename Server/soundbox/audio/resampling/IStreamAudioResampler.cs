namespace Soundbox.Audio
{
    /// <summary>
    /// Resampler for uncompressed audio streams: takes samples from an <see cref="IStreamAudioSource"/> and transforms them
    /// into a different <see cref="WaveStreamAudioFormat"/>.<br/>
    /// This interface extends <see cref="IStreamAudioSource"/> and acts mostly as a marker: doesn't really do anything by itself.<br/>
    /// The <see cref="IStreamAudioSource.Start"/> and <see cref="IStreamAudioSource.Stop"/>
    /// methods simply call through to the input stream. The resampler does not need to be started explicitly, if the input source is started already.<br/>
    /// <see cref="System.IDisposable.Dispose"/> also calls through.
    /// </summary>
    public interface IStreamAudioResampler : IWrappedStreamAudioSource
    {
    }
}
