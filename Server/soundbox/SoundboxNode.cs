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
    public class SoundboxNode
    {
        public Guid ID;
        /// <summary>
        /// Display name
        /// </summary>
        public string Name;
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

        private ICollection<string> _tags;
        /// <summary>
        /// List of tags/categories to easily find similar files. E.g. "Star Wars", "Funny", "Meme"...
        /// </summary>
        public ICollection<string> Tags
        {
            get
            {
                if (_tags == null)
                    _tags = new List<string>();
                return _tags;
            }
            set => _tags = value;
        }

        #endregion

        /// <summary>
        /// A dummy ID that can be used to indicate that a new item should be created. Value: 00000000-0000-0000-0000-000000000000<br/>
        /// Parsers should set this value when no <see cref="ID"/> is found while parsing (which they should do automatically since this is <see cref="Guid"/>'s default value).
        /// </summary>
        public static readonly Guid ID_DEFAULT_NEW_ITEM = default;

        /// <summary>
        /// Returns true if this item represents a new item that a client wants to create.
        /// </summary>
        /// <returns></returns>
        public bool IsNew()
        {
            return ID == ID_DEFAULT_NEW_ITEM;
        }

        public override bool Equals(object obj)
        {
            return obj is SoundboxNode file &&
                   ID.Equals(file.ID);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ID);
        }

        public static bool operator ==(SoundboxNode left, SoundboxNode right)
        {
            return EqualityComparer<SoundboxNode>.Default.Equals(left, right);
        }

        public static bool operator !=(SoundboxNode left, SoundboxNode right)
        {
            return !(left == right);
        }
    }
}
