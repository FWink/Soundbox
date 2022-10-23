using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Represents options for the playback of a single sound (e.g. volume, pitch...)
    /// </summary>
    public class PlaybackOptions
    {
        /// <summary>
        /// Static volume on a 1-100 scale (relative to the overall sound volume).
        /// </summary>
        public int Volume;

        /// <summary>
        /// Playback speed which affects the pitch as well: doubling the speed increases the pitch by an octave (1.0 == default speed and pitch).
        /// </summary>
        public double SpeedPitch;

        /// <summary>
        /// When the sound is being played as part of a chain of playbacks this denotes a break/delay between the end of the current and the start of the next sound.
        /// May be negative.
        /// </summary>
        public int ChainDelayMs;

        /// <summary>
        /// Whether to clip the current sound for a negative <see cref="ChainDelayMs"/>. False: sounds may overlap.
        /// </summary>
        public bool ChainDelayClip;

        /// <summary>
        /// Checks for various (possibly malicious) misconfigurations and attempts to repair them to some degree.
        /// </summary>
        /// <returns>
        /// True if the sanity check was OK and the options may be processed.
        /// </returns>
        public bool SanityCheck()
        {
            if (Volume <= Constants.VOLUME_MIN)
                return false;
            if (Volume > Constants.VOLUME_MAX)
                Volume = Constants.VOLUME_MAX;
            if (!double.IsNormal(SpeedPitch))
                return false;
            return true;
        }

        public static PlaybackOptions Default()
        {
            var options = new PlaybackOptions();
            options.Volume = Constants.VOLUME_MAX;
            options.SpeedPitch = 1;
            options.ChainDelayMs = 0;
            options.ChainDelayClip = false;

            return options;
        }
    }
}
