using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Represents either a <see cref="Sound"/> or a <see cref="SoundboxDirectory"/>. Or more formally: represents a node in a tree of files.
    /// </summary>
    public class SoundboxFile
    {
        public Guid ID;
        /// <summary>
        /// Display name
        /// </summary>
        public string Name;
        /// <summary>
        /// File name in directory
        /// </summary>
        public string FileName;
        /// <summary>
        /// Absolute file name (in the soundbox context, i.e. path is relative to soundbox root directory)
        /// </summary>
        public string AbsoluteFileName;
        /// <summary>
        /// Parent directory. Not-null
        /// TODO during serialization make sure we get a depth of 1 here.
        /// TODO Don't serialize this for persistent storage
        /// </summary>
        public SoundboxDirectory ParentDirectory;

        #region Extras

        /// <summary>
        /// Absolute URL for the file's icon/image.
        /// </summary>
        public string IconUrl;

        /// <summary>
        /// List of tags/categories to easily find similar files. E.g. "Star Wars", "Funny", "Meme"...
        /// </summary>
        public ICollection<string> Tags;

        #endregion
    }
}
