using JsonSubTypes;
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
    [JsonConverter(typeof(JsonSubtypes))]
    [JsonSubtypes.KnownSubTypeWithProperty(typeof(SoundboxDirectory), "Children")]
    [JsonSubtypes.FallBackSubType(typeof(Sound))]
    public class SoundboxNode
    {
        public Guid ID { get; set; }
        /// <summary>
        /// Display name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Parent directory. Not-null
        /// TODO Don't serialize this for persistent storage
        /// </summary>
        public SoundboxDirectory ParentDirectory { get; set; }

        #region Extras

        /// <summary>
        /// Absolute URL for the file's icon/image.
        /// </summary>
        public string IconUrl { get; set; }

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

        #region "Serialization"

        /// <summary>
        /// Must be set to true in <see cref="Flatten(bool)"/>. False will cause the serializer to skip referenced nodes (such as <see cref="ParentDirectory"/>).
        /// </summary>
        protected bool Flattened;

        /// <summary>
        /// Flattens the node and returns a copy that does not link to any other node (such as <see cref="SoundboxNode.ParentDirectory"/>).
        /// This is often used when returning files to a client when only a restricted set of information should be passed instead of the entire file tree.
        /// </summary>
        /// <param name="withParent">
        /// True: the <see cref="ParentDirectory"/> should be returned as well in a flattened form.
        /// </param>
        /// <returns></returns>
        public virtual SoundboxNode Flatten(bool withParent = false)
        {
            throw new NotImplementedException("Flatten is not implemented in \"abstract\" base class SoundboxNode");
        }

        public bool ShouldSerializeParentDirectory()
        {
            return Flattened && ParentDirectory != null;
        }

        #endregion

        #region "Copy"

        /// <summary>
        /// Creates a deep-ish copy of this node: simple properties such as lists of strings are deep copied,
        /// while referenced nodes are shallow copied.
        /// This results in a copy that is safe to use for before/after comparisons.
        /// </summary>
        /// <returns></returns>
        public virtual SoundboxNode CompareCopy()
        {
            var copy = new SoundboxNode();
            CompareCopyFill(copy);
            return copy;
        }

        /// <summary>
        /// Helper for <see cref="CompareCopy"/>: fills the given copy with our copied properties.
        /// </summary>
        /// <param name="other"></param>
        protected virtual void CompareCopyFill(SoundboxNode other)
        {
            other.ID = this.ID;
            other.Name = this.Name;
            other.IconUrl = this.IconUrl;
            if (this._tags != null)
                other._tags = new List<string>(this._tags);
            other.Flattened = this.Flattened;
            //shallow copy:
            other.ParentDirectory = this.ParentDirectory;
        }

        #endregion

        #region "Equality/HashCode"
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
        #endregion
    }
}
