using Newtonsoft.Json;
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
        [JsonIgnore]
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

        /// <summary>
        /// A dummy ID that can be used to indicate that a new item should be created. Value: 00000000-0000-0000-0000-000000000000<br/>
        /// Parsers should set this value when no <see cref="ID"/> is found while parsing (which they should do automatically since this is <see cref="Guid"/>'s default value).
        /// </summary>
        public static readonly Guid ID_DEFAULT_NEW_ITEM = default;

        /// <summary>
        /// Updates the <see cref="AbsoluteFileName"/> after a change to <see cref="FileName"/> or <see cref="ParentDirectory"/>.
        /// </summary>
        public void UpdateAbsoluteFileName()
        {
            if(ParentDirectory != null && !ParentDirectory.IsRootDirectory())
            {
                string absoluteFileName = ParentDirectory.AbsoluteFileName;
                if (!absoluteFileName.EndsWith("/"))
                    absoluteFileName += "/";
                absoluteFileName += FileName;

                this.AbsoluteFileName = absoluteFileName;
            }
            else
            {
                this.AbsoluteFileName = this.FileName;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is SoundboxFile file &&
                   ID.Equals(file.ID);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }

        public static bool operator ==(SoundboxFile left, SoundboxFile right)
        {
            return EqualityComparer<SoundboxFile>.Default.Equals(left, right);
        }

        public static bool operator !=(SoundboxFile left, SoundboxFile right)
        {
            return !(left == right);
        }
    }
}
