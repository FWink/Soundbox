using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Client request to play one or more sounds in a chain.
    /// </summary>
    public class SoundPlaybackRequest
    {
        public IList<SoundPlayback> Sounds;
    }
}
