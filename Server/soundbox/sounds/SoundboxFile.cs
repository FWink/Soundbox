using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundboxFile : SoundboxNode
    {
        /// <summary>
        /// File name in directory
        /// </summary>
        public string FileName;
        /// <summary>
        /// Absolute file name (in the soundbox context, i.e. path is relative to soundbox root directory)
        /// </summary>
        [JsonIgnore]
        public string AbsoluteFileName;

        public bool ShouldSerializeFileName()
        {
            return false;
        }
    }
}
