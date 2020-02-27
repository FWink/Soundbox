using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Util
{
    public static class Collections
    {
        /// <summary>
        /// Constructs a new <see cref="ISet{T}"/> using the current dictionary's <see cref="IEqualityComparer"/>. The returned Set is not backed by the dictionary.
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dictionary"></param>
        /// <returns></returns>
        public static ISet<K> ToSet<K, V>(this Dictionary<K, V> dictionary)
        {
            return new HashSet<K>(dictionary.Keys, dictionary.Comparer);
        }

        /// <summary>
        /// Adds all items from the given <see cref="IEnumerable"/> to the current collection.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="collection"></param>
        /// <param name="other"></param>
        public static void AddAll<V>(this ICollection<V> collection, IEnumerable<V> other)
        {
            var list = collection as List<V>;
            if(list != null)
            {
                list.AddRange(other);
            }
            else
            {
                foreach(var item in other)
                {
                    collection.Add(item);
                }
            }
        }
    }
}
