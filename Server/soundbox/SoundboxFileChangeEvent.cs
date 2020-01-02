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
        /// Directory the file has been moved to.
        /// </summary>
        public SoundboxDirectory ToDirectory;
    }
}
