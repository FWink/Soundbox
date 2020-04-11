using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Empty interface that marks an <see cref="IVolumeService"/> as a service that cooperates with an available <see cref="IVirtualVolumeService"/> if any.
    /// If the configured <see cref="IVolumeService"/> does not implement <see cref="IVirtualVolumeServiceCoop"/> then no <see cref="IVirtualVolumeService"/> should be provided.
    /// </summary>
    public interface IVirtualVolumeServiceCoop
    {
    }
}
