using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundEventArgs : EventArgs
    {
        public readonly Sound Sound;

        public SoundEventArgs(Sound sound)
        {
            this.Sound = sound;
        }
    }
}
