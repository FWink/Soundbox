using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class Sound : SoundboxFile, ISoundboxPlayable
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

        public SoundboxVoiceActivation VoiceActivation { get; set; }

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
                VoiceActivation = this.VoiceActivation,
                Flattened = true
            };
            if (withParent && this.ParentDirectory != null)
                flattened.ParentDirectory = this.ParentDirectory.Flatten() as SoundboxDirectory;
            return flattened;
        }

        #region "Copy"

        public override SoundboxNode CompareCopy()
        {
            var other = new Sound();
            CompareCopyFill(other);
            return other;
        }

        protected override void CompareCopyFill(SoundboxNode other)
        {
            base.CompareCopyFill(other);
            if (other is Sound sound)
            {
                if (this._metaData != null)
                    sound._metaData = new SoundMetaData(this._metaData);
                if (this.VoiceActivation != null)
                    sound.VoiceActivation = new SoundboxVoiceActivation(this.VoiceActivation);
            }
        }

        #endregion
    }
}
