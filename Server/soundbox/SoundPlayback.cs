using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Request by the client for a single sound.
    /// </summary>
    public class SoundPlayback
    {
        public Sound Sound;
        private PlaybackOptions _options;

        public PlaybackOptions Options
        {
            get
            {
                if (_options == null)
                    _options = PlaybackOptions.Default();
                return _options;
            }
            set => _options = value;
        }

        /// <summary>
        /// Returns the actual, full playback length of the <see cref="Sound"/> in ms while factoring in:<list type="bullet">
        /// <item><see cref="Sound.Length"/></item>
        /// <item><see cref="PlaybackOptions.SpeedPitch"/></item>
        /// </list>
        /// </summary>
        /// <returns></returns>
        public int GetActualLength()
        {
            return (int) (Sound.Length / Options.SpeedPitch);
        }
    }
}
