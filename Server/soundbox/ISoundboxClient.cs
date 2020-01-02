using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public interface ISoundboxClient
    {
        /// <summary>
        /// Called whenever the tree of available sounds changes.
        /// </summary>
        /// <param name="changeEvent"></param>
        /// <returns></returns>
        Task OnFileEvent(SoundboxFileChangeEvent changeEvent);

        /// <summary>
        /// Called whenever the list of currently playing sounds changes (i.e. when at least one sound starts/stops playing).
        /// </summary>
        /// <param name="playingNow"></param>
        /// <returns></returns>
        Task OnSoundsPlayingChanged(ICollection<PlayingNow> playingNow);

        /// <summary>
        /// Called when the current volume level changes (<see cref="SoundboxHub.SetVolume(int)"/>).
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        Task OnVolumeChanged(int volume);

        /// <summary>
        /// Called when the maximum system volume setting changes (<see cref="SoundboxHub.SetSettingMaxVolume(int)"/>).
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        Task OnSettingMaxVolumeChanged(int volume);
    }
}
