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
        public string FileName { get; set; }
        /// <summary>
        /// Absolute file name (in the soundbox context, i.e. path is relative to soundbox root directory)
        /// </summary>
        [JsonIgnore]
        public string AbsoluteFileName { get; set; }

        public bool ShouldSerializeFileName()
        {
            return false;
        }

        #region "Copy"

        public override SoundboxNode CompareCopy()
        {
            var other = new SoundboxFile();
            CompareCopyFill(other);
            return other;
        }

        protected override void CompareCopyFill(SoundboxNode other)
        {
            base.CompareCopyFill(other);
            if (other is SoundboxFile file)
            {
                file.FileName = this.FileName;
                file.AbsoluteFileName = this.AbsoluteFileName;
            }
        }

        #endregion
    }
}
