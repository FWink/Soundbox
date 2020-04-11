using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DummyVolumeService : IVolumeService, IVirtualVolumeServiceCoop
    {
        private const string PREFERENCES_KEY_VOLUME_DUMMY = "Soundbox.Volume.Dummy";

        private IPreferencesProvider<double> Preferences;

        private IServiceProvider ServiceProvider;

        public DummyVolumeService(IPreferencesProvider<double> preferences, IServiceProvider serviceProvider)
        {
            this.Preferences = preferences;
            this.ServiceProvider = serviceProvider;
        }

        public async Task<double> GetVolume()
        {
            var virtualVolume = GetVirtualVolumeService();
            if(virtualVolume != null)
            {
                return await virtualVolume.GetVolume();
            }

            if (!await Preferences.Contains(PREFERENCES_KEY_VOLUME_DUMMY))
                return Constants.VOLUME_MAX;
            return await Preferences.Get(PREFERENCES_KEY_VOLUME_DUMMY);
        }

        public Task SetVolume(double volume)
        {
            var virtualVolume = GetVirtualVolumeService();
            if (virtualVolume != null)
            {
                return virtualVolume.SetVolume(volume);
            }

            return Preferences.Set(PREFERENCES_KEY_VOLUME_DUMMY, volume);
        }

        /// <summary>
        /// Returns the installed <see cref="IVirtualVolumeService"/> if any.
        /// </summary>
        /// <returns></returns>
        protected IVirtualVolumeService GetVirtualVolumeService()
        {
            return ServiceProvider.GetService(typeof(IVirtualVolumeService)) as IVirtualVolumeService;
        }
    }
}
