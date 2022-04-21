using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Audio
{
    /// <summary>
    /// Audio source that contains the name of an audio input device.
    /// Audio sources of this kind thus do not provide audio themselves, but require other code to access the audio device directly.
    /// </summary>
    public class DeviceAudioSource : IAudioSource
    {
        /// <summary>
        /// Name of the audio input device that should be used.
        /// </summary>
        public string AudioInputDeviceName { get; private set; }

        /// <summary>
        /// Alternative to <see cref="AudioInputDeviceName"/>: use the machine's default audio input device.
        /// </summary>
        public bool UseDefaultAudioInputDevice { get; private set; }

        #region "Static Getters"

        /// <summary>
        /// Returns a <see cref="DeviceAudioSource"/> for the audio device with the given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static DeviceAudioSource FromAudioDevice(string name)
        {
            return new DeviceAudioSource()
            {
                AudioInputDeviceName = name
            };
        }

        /// <summary>
        /// Returns a <see cref="DeviceAudioSource"/> for the machine's default audio input device.
        /// </summary>
        /// <returns></returns>
        public static DeviceAudioSource FromDefaultAudioDevice()
        {
            return new DeviceAudioSource()
            {
                UseDefaultAudioInputDevice = true
            };
        }

        #endregion

        #region "Equals/HashCode"

        public override bool Equals(object obj)
        {
            return obj is DeviceAudioSource source &&
                   AudioInputDeviceName == source.AudioInputDeviceName &&
                   UseDefaultAudioInputDevice == source.UseDefaultAudioInputDevice;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AudioInputDeviceName, UseDefaultAudioInputDevice);
        }

        public static bool operator ==(DeviceAudioSource left, DeviceAudioSource right)
        {
            return EqualityComparer<DeviceAudioSource>.Default.Equals(left, right);
        }

        public static bool operator !=(DeviceAudioSource left, DeviceAudioSource right)
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
