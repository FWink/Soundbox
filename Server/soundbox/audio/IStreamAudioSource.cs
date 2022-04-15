using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Audio
{
    /// <summary>
    /// Audio source that provides audio as a stream of bytes in some specified format (PCM with parameters for sample rate, bit depth etc; MP3;...)<br/>
    /// Doesn't actually do anything yet and is here only as a placeholder.
    /// </summary>
    public interface IStreamAudioSource : IAudioSource
    {
    }
}
