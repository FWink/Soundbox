using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// <see cref="ISoundPlaybackService"/> that can adjust its playback volume on-the-fly. Works in cooperation with <see cref="IVirtualVolumeService"/>.
    /// </summary>
    public interface ISoundPlaybackVirtualVolumeService : ISoundPlaybackService
    {
        /// <summary>
        /// Immediately adjusts the volume of the currently playing sound from a software side.<br/>
        /// Being a global setting, this does not override the volume supplied in <see cref="ISoundPlaybackService.Play(SoundboxContext, SoundPlayback)"/> but is instead
        /// combined with it via multiplication.
        /// </summary>
        /// <param name="volume"></param>
        void SetVolume(double volume);
    }
}
