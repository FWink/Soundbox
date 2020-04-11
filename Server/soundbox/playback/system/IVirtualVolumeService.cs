using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Optional service that cooperates with <see cref="IVolumeService"/> and <see cref="ISoundPlaybackVirtualVolumeService"/>.<br/>
    /// Startup configuration: a IVirtualVolumeService should only be provided if all installed <see cref="ISoundPlaybackService"/>s are <see cref="ISoundPlaybackVirtualVolumeService"/>s.<br/>
    /// Runtime usage: since IVirtualVolumeService is optional, it might not be available at runtime. Classes using IVirtualVolumeService should check for availability and adjust their behavior accordingly.<br/>
    /// These two axioms combined result in the optimal behavior for any <see cref="IVolumeService"/>: IVirtualVolumeService is available ? => use it to adjust the volume; otherwise set the volume on the OS level.
    /// </summary>
    /// <seealso cref="Volume.GetVolume(double, double)"/>
    /// <seealso cref="IVirtualVolumeServiceCoop"/>
    public interface IVirtualVolumeService
    {
        /// <summary>
        /// Immediately adjusts the playback volume of all currently active <see cref="ISoundPlaybackVirtualVolumeService"/>s.
        /// </summary>
        /// <param name="volume"></param>
        Task SetVolume(double volume);

        /// <summary>
        /// Returns the current virtual system volume.
        /// </summary>
        /// <returns></returns>
        Task<double> GetVolume();

        /// <summary>
        /// Register a new playback service instance with the volume service.
        /// The playback service's <see cref="ISoundPlaybackVirtualVolumeService.SetVolume(double)"/> method is called when the volume service's <see cref="SetVolume(int)"/> is called.<br/>
        /// Cleanup is performed automatically, un-registering is not required.
        /// </summary>
        /// <param name="playbackService"></param>
        void RegisterSoundPlayback(ISoundPlaybackVirtualVolumeService playbackService);
    }
}
