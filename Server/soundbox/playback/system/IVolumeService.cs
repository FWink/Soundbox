using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Modifies the current system output volume. Any sounds being played right now will immediately adjust their volume.<br/>
    /// "System volume" in that sense usually is the volume of the entire local machine.
    /// But it can just as well mean modifying the volume of all sounds playing right now in software if the installed <see cref="ISoundPlaybackService"/> supports such an operation.
    /// </summary>
    /// <seealso cref="IVirtualVolumeService"/>
    public interface IVolumeService
    {
        /// <summary>
        /// Sets the current system volume on a scale of 0-100 (inclusive).
        /// </summary>
        /// <param name="volume"></param>
        Task SetVolume(double volume);

        /// <summary>
        /// Returns the current overall system volume.
        /// </summary>
        /// <returns></returns>
        Task<double> GetVolume();
    }
}
