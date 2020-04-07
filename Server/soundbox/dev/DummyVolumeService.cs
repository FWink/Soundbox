using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DummyVolumeService : IVolumeService
    {
        protected double Volume = 100;

        public Task<double> GetVolume()
        {
            return Task.FromResult(Volume);
        }

        public Task SetVolume(double volume)
        {
            this.Volume = volume;
            return Task.FromResult(true);
        }
    }
}
