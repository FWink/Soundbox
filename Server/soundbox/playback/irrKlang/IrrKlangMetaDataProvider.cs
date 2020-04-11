using IrrKlang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Playback.IrrKlang
{
    /// <summary>
    /// <see cref="IMetaDataProvider"/> that uses <see cref="irrklang.ISoundEngine"/> (via <see cref="IIrrKlangEngineProvider"/>) to parse the meta data of sounds.
    /// </summary>
    public class IrrKlangMetaDataProvider : IMetaDataProvider
    {
        protected IIrrKlangEngineProvider EngineProvider;

        public IrrKlangMetaDataProvider(IIrrKlangEngineProvider engineProvider)
        {
            this.EngineProvider = engineProvider;
        }

        public async Task<SoundMetaData> GetMetaData(string filePath)
        {
            var engine = await EngineProvider.GetSoundEngine();
            var soundSource = engine.AddSoundSourceFromFile(filePath);

            if(soundSource != null)
            {
                var metaData = new SoundMetaData()
                {
                    Length = soundSource.PlayLength
                };

                //cleanup
                engine.RemoveSoundSource(soundSource.Name);

                return metaData;
            }

            //could not parse or something
            return null;
        }
    }
}
