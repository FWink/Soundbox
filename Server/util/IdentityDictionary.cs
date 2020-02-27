using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Util
{
    /// <summary>
    /// Similar to an IdentityHashMap in Java: compares keys via their identity ("address"). Thus it is possible to have multiple "equal" keys because they are distinct objects.
    /// </summary>
    public class IdentityDictionary<K,V> : Dictionary<K,V>
    {
        public IdentityDictionary() : base(new IdentityHashProvider<K>()) { }

        public IdentityDictionary(int capacity) : base(capacity, new IdentityHashProvider<K>()) { }

        public IdentityDictionary(IDictionary<K,V> dictionary) : base(dictionary, new IdentityHashProvider<K>()) { }
    }
}
