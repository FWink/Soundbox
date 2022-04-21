using System;

namespace Soundbox.Audio
{
    /// <summary>
    /// Represents a generic audio output facility. This interface doesn't do anything by itself,
    /// but needs to be cast either to <see cref="AudioDevice"/> or <see cref="IStreamAudioSink"/>.
    /// Methods having audio sinks as parameters either need to decide if the passed audio sink is supported,
    /// or they need to directly specify the concrete type they support.
    /// </summary>
    public interface IAudioSink : IDisposable
    {
    }
}
