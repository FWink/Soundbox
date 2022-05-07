namespace Soundbox.Audio
{
    /// <summary>
    /// Cause why an <see cref="IStreamAudioSource"/> stopped.
    /// </summary>
    /// <seealso cref="IStreamAudioSource.Stopped"/>
    public enum StreamAudioSourceStoppedCause
    {
        Unknown = 0,
        Exception,
        /// <summary>
        /// <see cref="IStreamAudioSource.Stop"/> has been called.
        /// </summary>
        Stopped,
        /// <summary>
        /// Primarily for <see cref="AudioBlob"/>s: end of stream has been reached.
        /// </summary>
        End
    }
}
