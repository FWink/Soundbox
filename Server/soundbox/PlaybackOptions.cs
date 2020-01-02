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
        /// Static volume on a 0-100 scale (relative to the overall sound volume).
        /// </summary>
        public int Volume;

        /// <summary>
        /// Playback speed which affects the pitch as well: doubling the speed increases the pitch by an octave.
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
    }
}
