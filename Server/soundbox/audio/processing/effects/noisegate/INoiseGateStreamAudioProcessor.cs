using System.Threading.Tasks;

namespace Soundbox.Audio.Processing.Noisegate
{
    /// <summary>
    /// Wraps a <see cref="IStreamAudioSource"/> and cuts off audio samples that are below a certain threshold.
    /// See also <see cref="NoiseGateStreamAudioProcessorOptions"/>.
    /// </summary>
    public interface INoiseGateStreamAudioProcessor : IWrappedStreamAudioSource
    {
        /// <summary>
        /// Applies/updates the noise gate's options.
        /// </summary>
        /// <param name="options"></param>
        /// <returns>
        /// Task that resolves once the new options are in effect.
        /// </returns>
        Task SetOptions(NoiseGateStreamAudioProcessorOptions options);

        /// <summary>
        /// Optional operation: hints to the noise gate, that the consumer detected that the audio stream probably dropped below the threshold level.
        /// This may cause the noise gate to kick in early instead of waiting for <see cref="NoiseGateStreamAudioProcessorOptions.Delay"/>.<br/>
        /// Example: a speech recognizer may detect that no one is speaking anymore => the noise gate may cut off right away.
        /// </summary>
        public virtual void OnAudioStop() { }
    }
}
