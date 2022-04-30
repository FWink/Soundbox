namespace Soundbox.Audio.Processing.Noisegate
{
    public interface INoiseGateStreamAudioProcessProvider
    {
        /// <summary>
        /// Constructs a noise gate that is compatible with the given stream's format.
        /// Call <see cref="INoiseGateStreamAudioProcessor.SetOptions(NoiseGateStreamAudioProcessorOptions)"/> afterwards.
        /// </summary>
        /// <param name="audioSource"></param>
        /// <returns>
        /// Null: the input format is not supported.
        /// </returns>
        INoiseGateStreamAudioProcessor GetNoiseGate(IStreamAudioSource audioSource);
    }
}
