using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DefaultVirtualVolumeService : IVirtualVolumeService
    {
        private const string PREFERENCES_KEY_VOLUME = "Soundbox.Volume.Virtual";
        private IPreferencesProvider<double> Preferences;

        private double Volume = double.NaN;

        /// <summary>
        /// Collects sounds that will get updated in <see cref="SetVolume(double)"/>.
        /// </summary>
        private readonly IList<WeakReference<ISoundPlaybackVirtualVolumeService>> Sounds = new List<WeakReference<ISoundPlaybackVirtualVolumeService>>();

        public DefaultVirtualVolumeService(IPreferencesProvider<double> preferences)
        {
            this.Preferences = preferences;
        }

        /// <summary>
        /// Returns a <see cref="IPreferencesProvider{T}"/> to store the volume choice persistently.
        /// </summary>
        /// <returns></returns>
        protected IPreferencesProvider<double> GetPreferences()
        {
            return Preferences;
        }

        public async Task<double> GetVolume()
        {
            if(!double.IsFinite(Volume))
            {
                //load from preferences
                if(await GetPreferences().Contains(PREFERENCES_KEY_VOLUME))
                {
                    Volume = await GetPreferences().Get(PREFERENCES_KEY_VOLUME);
                }

                Volume = Util.Volume.Limit(Volume);
            }

            return Volume;
        }

        public void RegisterSoundPlayback(ISoundPlaybackVirtualVolumeService playbackService)
        {
            var soundRef = new WeakReference<ISoundPlaybackVirtualVolumeService>(playbackService);
            lock (Sounds)
            {
                Sounds.Add(soundRef);
            }

            //cleanup when the playback is done
            playbackService.PlaybackFinished += (e, args) =>
            {
                lock(Sounds)
                {
                    Sounds.Remove(soundRef);
                }
            };
        }

        public async Task SetVolume(double volume)
        {
            this.Volume = volume;
            //store
            await GetPreferences().Set(PREFERENCES_KEY_VOLUME, volume);

            //update all our sounds
            lock(Sounds)
            {
                for(int i = 0; i < Sounds.Count; ++i)
                {
                    if(Sounds[i].TryGetTarget(out var sound))
                    {
                        sound.SetVolume(volume);
                    }
                    else
                    {
                        //cleanup
                        Sounds.RemoveAt(i);
                        --i;
                    }
                }
            }
        }
    }
}
