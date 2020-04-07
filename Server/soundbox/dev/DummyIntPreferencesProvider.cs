using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DummyIntPreferencesProvider : IPreferencesProvider<int>
    {
        public Task<bool> Contains(string key)
        {
            return Task.FromResult(false);
        }

        public Task Delete(string key)
        {
            return Task.FromResult(true);
        }

        public Task<int> Get(string key)
        {
            throw new NotImplementedException();
        }

        public Task Set(string key, int value)
        {
            return Task.FromResult(true);
        }
    }
}
