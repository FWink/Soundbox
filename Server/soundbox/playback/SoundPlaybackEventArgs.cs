using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundPlaybackEventArgs : SoundEventArgs
    {
        public readonly SoundPlayback SoundPlayback;

        public SoundPlaybackEventArgs(SoundPlayback soundPlayback) : base(soundPlayback.Sound)
        {
            SoundPlayback = soundPlayback;
        }
    }
}
