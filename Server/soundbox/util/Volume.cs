using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
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
            return volume * modifier / 100;
        }

        /// <summary>
        /// Reverse of <see cref="GetVolume(int, int)"/>: calculates the volume passed to <see cref="GetVolume(double, double)"/> based on the given result and the applied modifier.
        /// </summary>
        /// <param name="volumeModified"></param>
        /// <param name="modifier"></param>
        /// <returns></returns>
        public static double GetVolumeOriginal(double volumeModified, double modifier)
        {
            return volumeModified * 100 / modifier;
        }
    }
}
