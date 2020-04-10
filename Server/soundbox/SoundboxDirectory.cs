using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundboxDirectory : SoundboxNode
    {
        public ICollection<SoundboxNode> Children = new List<SoundboxNode>();

        /// <summary>
        /// Unique watermark that gets updated whenever a change in <see cref="Children"/>
        /// occurs (recursive). Example:<br/>
        /// Structure: /my/directory/<br/>
        /// When a new sound is added these directorys all get a new (identical) watermark:<list type="bullet">
        /// <item>root</item>
        /// <item>my</item>
        /// <item>directory</item>
        /// </list>
        /// However a sibling of 'directory' is unaffected.
        /// </summary>
        public Guid Watermark;

        public void AddChild(SoundboxNode file)
        {
            this.Children.Add(file);
            file.ParentDirectory = this;
        }

        public void AddChildren(IEnumerable<SoundboxNode> files)
        {
            foreach(var child in files)
            {
                AddChild(child);
            }
        }

        public bool IsRootDirectory()
        {
            return ParentDirectory == null;
        }
    }
}
