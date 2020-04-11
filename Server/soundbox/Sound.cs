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
    }
}
