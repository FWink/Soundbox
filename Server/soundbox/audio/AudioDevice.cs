using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Audio
{
    /// <summary>
    /// Audio source/sink that contains the name of an audio device.
    /// Audio sources/sinks of this kind thus do not provide any audio functions themselves, but require other code to access the audio device directly.
    /// I.e., this is used for configuration only.
    /// </summary>
    public class AudioDevice : IAudioSource, IAudioSink
    {
        /// <summary>
        /// Name of the audio input device that should be used.
        /// </summary>
        public string AudioDeviceName { get; private set; }

        /// <summary>
        /// Alternative to <see cref="AudioDeviceName"/>: use the machine's default audio input device.
        /// </summary>
        public bool UseDefaultAudioDevice { get; private set; }

        #region "Static Getters"

        /// <summary>
        /// Returns a <see cref="AudioDevice"/> for the audio device with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AudioDevice FromAudioDevice(string name)
        {
            return new AudioDevice()
            {
                AudioDeviceName = name
            };
        }

        /// <summary>
        /// Returns a <see cref="AudioDevice"/> for the machine's default audio input device.
        /// </summary>
        /// <returns></returns>
        public static AudioDevice FromDefaultAudioDevice()
        {
            return new AudioDevice()
            {
                UseDefaultAudioDevice = true
            };
        }

        #endregion

        #region "Equals/HashCode"

        public override bool Equals(object obj)
        {
            return obj is AudioDevice source &&
                   AudioDeviceName == source.AudioDeviceName &&
                   UseDefaultAudioDevice == source.UseDefaultAudioDevice;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AudioDeviceName, UseDefaultAudioDevice);
        }

        public static bool operator ==(AudioDevice left, AudioDevice right)
        {
            return EqualityComparer<AudioDevice>.Default.Equals(left, right);
        }

        public static bool operator !=(AudioDevice left, AudioDevice right)
        {
            return !(left == right);
        }

        #endregion

        #region "Dispose"

        public void Dispose()
        {
            //nothing to do
        }

        #endregion
    }
}
