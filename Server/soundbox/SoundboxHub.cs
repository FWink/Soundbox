using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundboxHub : Hub<ISoundboxClient>
    {
        /// <summary>
        /// Returns the given directory's children (i.e. fetches <see cref="SoundboxDirectory.Children"/>).
        /// </summary>
        /// <param name="directory">The directory content to fetch. Null for base directory</param>
        /// <param name="recursive">Whether to recursively fetch the entire file branch.</param>
        /// <returns>
        /// The directory's content. If null is passed for the <paramref name="directory"/> (requesting the root directory) and <paramref name="recursive"/> is false
        /// then only the root directory is returned without its children. That can be utilized to quickly verify whether the local sound list is up-to-date by checking the root directory's <see cref="SoundboxDirectory.Watermark"/>.
        /// </returns>
        public async Task<ICollection<SoundboxFile>> GetSounds(SoundboxDirectory directory = null, bool recursive = false)
        {
            return new List<SoundboxFile>();
        }

        /// <summary>
        /// Returns all sounds that are currently being played.
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<PlayingNow>> GetSoundsPlayingNow()
        {
            return null;
        }

        /// <summary>
        /// Plays a single or multiple sounds.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task Play(SoundPlaybackRequest request)
        {

        }

        /// <summary>
        /// Gets the current volume level (<see cref="SetVolume(int)"/>).
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetVolume()
        {
            return 100;
        }

        /// <summary>
        /// Sets the current volume on a scale from 0-100
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public async Task SetVolume(int volume)
        {

        }

        /// <summary>
        /// Gets the current maximum system volume (<see cref="SetSettingMaxVolume(int)"/>).
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetSettingMaxVolume()
        {
            return 100;
        }

        /// <summary>
        /// Sets the maximum system volume (0-100). This affects the effect of <see cref="SetVolume(int)"/>:
        /// the value given in <see cref="SetVolume(int)"/> is mapped to a scale from 0-MAXVOLUME instead
        /// (e.g. if both are 50 then the actual volume is 25).
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public async Task SetSettingMaxVolume(int volume)
        {

        }

        /// <summary>
        /// Immediately stops all playback.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {

        }

        /// <summary>
        /// Deletes the given sound or directory. When a directory is passed then all content is deleted recursively.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task Delete(SoundboxFile file)
        {

        }

        /// <summary>
        /// Edits the given file. Currently these attributes are affected:<list type="bullet">
        /// <item><see cref="SoundboxFile.Name"/></item>
        /// <item><see cref="SoundboxFile.Tags"/></item>
        /// </list>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task Edit(SoundboxFile file)
        {

        }

        /// <summary>
        /// Moves a file to a new directory.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public async Task Move(SoundboxFile file, SoundboxDirectory directory)
        {

        }
    }
}
