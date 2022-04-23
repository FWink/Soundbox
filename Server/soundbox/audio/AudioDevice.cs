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
        /// Name of the audio device that should be used.
        /// </summary>
        public virtual string AudioDeviceName { get; private set; }

        /// <summary>
        /// Alternative to <see cref="AudioDeviceName"/>: use the machine's default audio input device.
        /// </summary>
        public virtual bool UseDefaultAudioInputDevice { get; private set; }

        /// <summary>
        /// Alternative to <see cref="AudioDeviceName"/>: use the machine's default audio output device.
        /// Note that it may be possible to use an output device as a loopback input device,
        /// thus returning audio that is rendered by the output device.
        /// </summary>
        public virtual bool UseDefaultAudioOutputDevice { get; private set; }

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
        /// Returns a <see cref="AudioDevice"/> for the machine's default audio input or output device.
        /// If both <paramref name="defaultInputDevice"/> and <paramref name="defaultOutputDevice"/> are false (default values),
        /// then they are both treated as true, thus returning a possibly valid AudioDevice when not passing any parameters.
        /// </summary>
        /// <returns></returns>
        public static AudioDevice FromDefaultAudioDevice(bool defaultInputDevice = false, bool defaultOutputDevice = false)
        {
            if (!defaultInputDevice && !defaultOutputDevice)
            {
                defaultInputDevice = true;
                defaultOutputDevice = true;
            }

            return new AudioDevice()
            {
                UseDefaultAudioInputDevice = defaultInputDevice,
                UseDefaultAudioOutputDevice = defaultOutputDevice
            };
        }

        #endregion

        #region "Equals/HashCode"

        public override bool Equals(object obj)
        {
            return obj is AudioDevice device &&
                   AudioDeviceName == device.AudioDeviceName &&
                   UseDefaultAudioInputDevice == device.UseDefaultAudioInputDevice &&
                   UseDefaultAudioOutputDevice == device.UseDefaultAudioOutputDevice;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AudioDeviceName, UseDefaultAudioInputDevice, UseDefaultAudioOutputDevice);
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
