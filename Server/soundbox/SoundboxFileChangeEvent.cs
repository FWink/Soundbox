using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundboxFileChangeEvent
    {
        public SoundboxFile File;
        public Type Event;
        /// <summary>
        /// The root <see cref="SoundboxDirectory.Watermark"/> before the change occurred.
        /// This can be used by clients to check if they missed any event.<br/>
        /// Note: the current watermark can be retrieved from <see cref="File"/>;
        /// either directly because it is a <see cref="SoundboxDirectory"/> or from its <see cref="SoundboxFile.ParentDirectory"/>.
        /// </summary>
        public Guid PreviousWatermark;

        public enum Type
        {
            ADDED,
            MODIFIED,
            DELETED,
            MOVED
        }
    }

    public class SoundboxFileMoveEvent : SoundboxFileChangeEvent
    {
        /// <summary>
        /// Directory the file has been moved from.
        /// </summary>
        public SoundboxDirectory FromDirectory;
    }
}
