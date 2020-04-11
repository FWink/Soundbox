using IrrKlang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Playback.IrrKlang
{
    /// <summary>
    /// Provides an <see cref="ISoundEngine"/> to playback services (<see cref="IrrKlangSoundPlaybackService"/>).
    /// </summary>
    public interface IIrrKlangEngineProvider
    {
        /// <summary>
        /// Returns a <see cref="ISoundEngine"/> that can be used to play sounds via irrKlang.
        /// </summary>
        /// <returns></returns>
        Task<ISoundEngine> GetSoundEngine();
    }
}
