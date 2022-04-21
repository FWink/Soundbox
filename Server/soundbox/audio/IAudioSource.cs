using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Audio
{
    /// <summary>
    /// Represents a generic source of audio. This interface doesn't do anything by itself,
    /// but needs to be cast either to <see cref="AudioDevice"/> or <see cref="IStreamAudioSource"/>.
    /// Methods having audio sources as parameters either need to decide if the passed audio source is supported,
    /// or they need to directly specify the concrete type they support.
    /// </summary>
    public interface IAudioSource : IDisposable
    {
    }
}
