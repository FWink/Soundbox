using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Some static helper methods to deal with the NAudio library.
    /// </summary>
    public static class NAudioUtilities
    {
        #region "Formats"

        /// <summary>
        /// Translates the given NAudio Wave format to our internal <see cref="WaveStreamAudioFormat"/>.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static WaveStreamAudioFormat FromNAudioWaveFormat(WaveFormat format)
        {
            if (format.Encoding == WaveFormatEncoding.Pcm)
            {
                return WaveStreamAudioFormat.GetIntFormat(format.SampleRate, format.BitsPerSample, format.Channels);
            }
            else if (format.Encoding == WaveFormatEncoding.IeeeFloat)
            {
                return WaveStreamAudioFormat.GetFloatFormat(format.SampleRate, format.BitsPerSample, format.Channels);
            }
            else
            {
                throw new ArgumentException($"Unknown NAudio WaveFormatEncoding: {format.Encoding}");
            }
        }

        #endregion

        #region "Devices"

        /// <summary>
        /// Searches for the given input/output device in NAudio's list of devices.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="directionInput">
        /// Tri-stated. True: input devices only. False: output devices only. Null: any device
        /// </param>
        /// <returns>
        /// Null if no matching device has been found.
        /// </returns>
        public static MMDevice GetDevice(AudioDevice device, bool? directionInput = null)
        {
            var enumerator = new MMDeviceEnumerator();
            MMDevice resultDevice = null;

            if (device.UseDefaultAudioOutputDevice)
            {
                //can be used for both input and output
                resultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                if (resultDevice != null)
                    return resultDevice;
            }

            if (device.UseDefaultAudioInputDevice && !(directionInput == false))
            {
                //input only
                resultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                if (resultDevice != null)
                    return resultDevice;
            }

            if (!string.IsNullOrWhiteSpace(device.AudioDeviceName))
            {
                //find by name or ID
                foreach (var candidate in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active))
                {
                    if (!DeviceNameMatches(candidate, device))
                        continue;

                    if (candidate.DataFlow == DataFlow.Render && (directionInput == null || directionInput == false))
                    {
                        return candidate;
                    }
                    else if (candidate.DataFlow == DataFlow.Capture && (directionInput == null || directionInput == true))
                    {
                        return candidate;
                    }
                }
            }

            return resultDevice;
        }

        /// <summary>
        /// True: the given NAudio device matches the name given in the <see cref="AudioDevice"/>
        /// </summary>
        /// <param name="naudioDevice"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private static bool DeviceNameMatches(MMDevice naudioDevice, AudioDevice device)
        {
            return device.AudioDeviceName.Equals(naudioDevice.ID) ||
                device.AudioDeviceName.Equals(naudioDevice.FriendlyName, StringComparison.CurrentCultureIgnoreCase) ||
                device.AudioDeviceName.Equals(naudioDevice.DeviceFriendlyName, StringComparison.CurrentCultureIgnoreCase);
        }

        #endregion
    }
}
