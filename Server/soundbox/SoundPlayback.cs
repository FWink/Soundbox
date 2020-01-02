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
        public PlaybackOptions Options;
    }
}
