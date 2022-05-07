namespace Soundbox.Audio
{
    /// <summary>
    /// Combines <see cref="IDeviceStreamAudioSourceProvider"/> and <see cref="IBlobStreamAudioSourceProvider"/>
    /// to return an <see cref="IStreamAudioSource"/> that reads either from an <see cref="AudioDevice"/> or an <see cref="AudioBlob"/>.
    /// </summary>
    public interface IStreamAudioSourceProvider
    {
        /// <summary>
        /// Returns an <see cref="IStreamAudioSource"/> that can read from the given <see cref="IAudioSource"/>.
        /// May return null when no <see cref="IStreamAudioSource"/> is available that can read this blob or device.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        IStreamAudioSource GetStreamAudioSource(IAudioSource source);
    }
}
