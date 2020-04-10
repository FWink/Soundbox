using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// <see cref="ServerResult"/> that is a response to single-file operations such as "upload a sound", "create a directory", "delete a sound"
    /// </summary>
    public class FileResult : ServerResult
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public SoundboxNode File
        {
            get;
        }

        /// <summary>
        /// The root watermark before the operation started.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Guid? PreviousWatermark
        {
            get;
        }

        /// <summary>
        /// For errors
        /// </summary>
        /// <param name="status"></param>
        public FileResult(ResultStatus status) : this(status, null, null) { }

        public FileResult(ResultStatus status, SoundboxNode file, Guid? previousWatermark) : base(status)
        {
            File = file;
            PreviousWatermark = previousWatermark;
        }
    }
}
