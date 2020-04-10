using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DummyVolumeService : IVolumeService
    {
        private const string PREFERENCES_KEY_VOLUME_DUMMY = "Soundbox.Volume.Dummy";

        private IPreferencesProvider<double> Preferences;

        public DummyVolumeService(IPreferencesProvider<double> preferences)
        {
            this.Preferences = preferences;
        }

        public async Task<double> GetVolume()
        {
            if (!await Preferences.Contains(PREFERENCES_KEY_VOLUME_DUMMY))
                return Constants.VOLUME_MAX;
            return await Preferences.Get(PREFERENCES_KEY_VOLUME_DUMMY);
        }

        public Task SetVolume(double volume)
        {
            return Preferences.Set(PREFERENCES_KEY_VOLUME_DUMMY, volume);
        }
    }
}
