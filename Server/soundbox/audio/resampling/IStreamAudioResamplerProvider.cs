namespace Soundbox.Audio
{
    /// <summary>
    /// Factory for <see cref="IStreamAudioResampler"/>s.
    /// </summary>
    public interface IStreamAudioResamplerProvider
    {
        /// <summary>
        /// Constructs a resampler that takes samples from <paramref name="input"/> and
        /// transforms them into the given <paramref name="outputFormat"/>.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outputFormat"></param>
        /// <returns>
        /// Null: the input or output format is not supported.
        /// </returns>
        IStreamAudioResampler GetResampler(IStreamAudioSource input, WaveStreamAudioFormat outputFormat);
    }
}
