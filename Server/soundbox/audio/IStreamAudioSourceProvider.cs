namespace Soundbox.Audio
{
    /// <summary>
    /// Provides <see cref="IStreamAudioSource"/>s from <see cref="AudioDevice"/>s.
    /// </summary>
    public interface IStreamAudioSourceProvider
    {
        /// <summary>
        /// Returns a <see cref="IStreamAudioSource"/> that can read from the given <see cref="AudioDevice"/>.
        /// May return null when no <see cref="IStreamAudioSource"/> is available that can read from this device
        /// (for example, because a loopback via <see cref="AudioDevice.UseDefaultAudioOutputDevice"/> is requested but not supported
        /// in the current configuration)
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        IStreamAudioSource GetStreamAudioSource(AudioDevice device);
    }
}
