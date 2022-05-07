namespace Soundbox.Audio
{
    /// <summary>
    /// Provides <see cref="IStreamAudioSource"/> from "blobs": byte streams that may or may not be compressed.
    /// </summary>
    public interface IBlobStreamAudioSourceProvider
    {
        /// <summary>
        /// Returns a <see cref="IStreamAudioSource"/> that can read from the given <see cref="AudioBlob"/>.
        /// May return null when no <see cref="IStreamAudioSource"/> is available that can read this blob,
        /// for example because no provider is installed that can decode the compressed audio.
        /// </summary>
        /// <param name="blob"></param>
        /// <returns></returns>
        IStreamAudioSource GetStreamAudioSource(AudioBlob blob);
    }
}
