using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        /// <summary>
        /// Compares two collections if they have the same values while disregarding differences in the order of the values.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="values1"></param>
        /// <param name="values2"></param>
        /// <returns></returns>
        public static bool CollectionsEqual<V>(this ICollection<V> values1, ICollection<V> values2)
        {
            if (Object.ReferenceEquals(values1, values2))
                return true;
            if (values1 == null || values2 == null)
                return false;
            if (values1.Count != values1.Count)
                return false;

            foreach(var val in values1)
            {
                if(!values2.Contains(val))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns only those values of the enumerable that match the given type.
        /// Basically a combination of using Linq.Where and Linq.Cast.
        /// </summary>
        /// <typeparam name="V"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<V> WhereIs<V>(this IEnumerable values)
        {
            foreach (var value in values)
            {
                if (value is V v)
                    yield return v;
            }
        }

        /// <summary>
        /// RNG used by <see cref="Shuffle{V}(IEnumerable{V})"/>
        /// </summary>
        private static readonly ThreadLocal<Random> ShuffleRandom = new ThreadLocal<Random>(() => new Random());

        /// <summary>
        /// Shuffles the given sequence of values.
        /// </summary>
        /// <remarks>
        /// Allocates a copy of the input list, thus requiring O(n) extra memory.
        /// </remarks>
        /// <typeparam name="V"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static IEnumerable<V> Shuffle<V>(this IEnumerable<V> values)
        {
            var list = values.ToList();
            var rng = ShuffleRandom.Value;

            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                V value = list[k];
                list[k] = list[n];
                list[n] = value;
            }

            return list;
        }
    }
}
