using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class Sound : SoundboxFile
    {
        private SoundMetaData _metaData;
        public SoundMetaData MetaData
        {
            get
            {
                if (_metaData == null)
                    _metaData = new SoundMetaData();
                return _metaData;
            }
            set => _metaData = value;
        }

        public override SoundboxNode Flatten(bool withParent = false)
        {
            var flattened =  new Sound()
            {
                ID = this.ID,
                Name = this.Name,
                IconUrl = this.IconUrl,
                Tags = this.Tags,
                ParentDirectory = null,
                AbsoluteFileName = this.AbsoluteFileName,
                FileName = this.FileName,
                _metaData = this._metaData,
                Flattened = true
            };
            if (withParent && this.ParentDirectory != null)
                flattened.ParentDirectory = this.ParentDirectory.Flatten() as SoundboxDirectory;
            return flattened;
        }
    }
}
