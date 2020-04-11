using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Playback.IrrKlang
{
    static class Utilities
    {
        /// <summary>
        /// Calculates the irrKlang volume (0-1) from the given soundbox volume (0-100)
        /// </summary>
        /// <param name="soundboxVolume"></param>
        /// <returns></returns>
        public static float GetVolume(double soundboxVolume)
        {
            return (float) ((soundboxVolume - Constants.VOLUME_MIN) / (Constants.VOLUME_MAX - Constants.VOLUME_MIN));
        }
    }
}
