using IrrKlang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soundbox.Playback.IrrKlang
{
    //TODO irrKlang seems to cache all sounds it plays. we need to investigate that and see if we can setup a memory limit
    public class DefaultIrrKlangEngineProvider : IIrrKlangEngineProvider, IDisposable, IVolumeService
    {
        protected bool Initialized = false;
        protected readonly ISoundEngine Engine;

        public DefaultIrrKlangEngineProvider(IPreferencesProvider<double> preferences)
        {
            this.Preferences = preferences;

            //add our BIN directory to our local path: need to load MP3 and flac plugins
            //not entirely sure why both SetEnvironmentVariable and LoadPlugins are required. doesn't work in debug mode without LoadPlugins
            string path = System.Reflection.Assembly.GetEntryAssembly().Location;
            path = new Regex(@"[^\\]+$").Replace(path, "");
            string pluginPath = path;

            path = System.Environment.GetEnvironmentVariable("Path") + ";" + path;
            System.Environment.SetEnvironmentVariable("Path", path);

            //start the sound engine
            Engine = new ISoundEngine();
            Engine.LoadPlugins(pluginPath);
        }

        public async Task<ISoundEngine> GetSoundEngine()
        {
            if(!Initialized)
            {
                //load and set volume before first playback
                await GetVolume();
                lock(this)
                {
                    //double check
                    if(!Initialized)
                    {
                        SetEngineVolume(Volume);
                        Initialized = true;
                    }
                }
            }

            return Engine;
        }

        public void Dispose()
        {
            Engine.Dispose();
        }

        #region "Volume Management"

        protected const string PREFERENCES_KEY_VOLUME = "Soundbox.Volume.IrrKlang";

        /// <summary>
        /// For volume storage.
        /// </summary>
        protected readonly IPreferencesProvider<double> Preferences;

        protected double Volume = double.NaN;

        /// <summary>
        /// Sets the sound engine's global volume <see cref="ISoundEngine.SoundVolume"/>
        /// </summary>
        /// <param name="soundboxVolume"></param>
        protected void SetEngineVolume(double soundboxVolume)
        {
            this.Engine.SoundVolume = Utilities.GetVolume(soundboxVolume);
        }

        public Task SetVolume(double volume)
        {
            this.Volume = volume;
            //set global engine volume
            SetEngineVolume(volume);

            //store persistently
            return Preferences.Set(PREFERENCES_KEY_VOLUME, volume);
        }

        public async Task<double> GetVolume()
        {
            if(!double.IsFinite(Volume))
            {
                if(await Preferences.Contains(PREFERENCES_KEY_VOLUME))
                {
                    Volume = await Preferences.Get(PREFERENCES_KEY_VOLUME);
                    Volume = Util.Volume.Limit(Volume);
                }
                else
                {
                    Volume = Constants.VOLUME_MAX;
                }
            }

            return Volume;
        }
        #endregion
    }
}
