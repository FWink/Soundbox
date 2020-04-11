using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Util
{
    public static class Volume
    {
        /// <summary>
        /// Applies the given muliplicative modifier to a volume.
        /// Effectively limits the given volume to the value of the modifier;
        /// passing a volume of 100 will return the modifier, while any lower volume is proportionally smaller.
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static double GetVolume(double volume, double modifier)
        {
            volume -= Constants.VOLUME_MIN;
            modifier -= Constants.VOLUME_MIN;

            return (volume * modifier / (Constants.VOLUME_MAX - Constants.VOLUME_MIN)) + Constants.VOLUME_MIN;
        }

        /// <summary>
        /// Reverse of <see cref="GetVolume(int, int)"/>: calculates the volume passed to <see cref="GetVolume(double, double)"/> based on the given result and the applied modifier.
        /// </summary>
        /// <param name="volumeModified"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static double GetVolumeOriginal(double volumeModified, double modifier)
        {
            volumeModified -= Constants.VOLUME_MIN;
            modifier -= Constants.VOLUME_MIN;

            if (modifier == 0)
                return Constants.VOLUME_MAX;

            return (volumeModified * (Constants.VOLUME_MAX - Constants.VOLUME_MIN) / modifier) + Constants.VOLUME_MIN;
        }

        /// <summary>
        /// Limits the given volume to our maximum values <see cref="Constants.VOLUME_MAX"/> and <see cref="Constants.VOLUME_MIN"/>.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public static double Limit(double volume)
        {
            if (!double.IsFinite(volume))
                return Constants.VOLUME_MAX;
            if (volume > Constants.VOLUME_MAX)
                return Constants.VOLUME_MAX;
            if (volume < Constants.VOLUME_MIN)
                return Constants.VOLUME_MIN;
            return volume;
        }
    }
}
