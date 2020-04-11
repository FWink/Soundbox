using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Contains meta data for a sound file, such as playback length or possibly bitrate, sampling rate, average volume level....
    /// </summary>
    public class SoundMetaData
    {
        private long _length;

        /// <summary>
        /// The sound's play length at 100% speed in ms.
        /// </summary>
        public long Length
        {
            get
            {
                if (_length < 0)
                    return 0;
                return _length;
            }
            set => _length = value;
        }

        /// <summary>
        /// True if the meta data contains a valid <see cref="Length"/>.
        /// </summary>
        public bool HasLength
        {
            get => Length > 0;
        }
    }
}
