using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Database service for simple key-value storage.
    /// </summary>
    interface IPreferencesProvider<T>
    {
        /// <summary>
        /// Stores a value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task Set(string key, T value);

        /// <summary>
        /// Removes a key from the storage.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task Delete(string key);

        /// <summary>
        /// Returns the stored value for the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<T> Get(string key);

        /// <summary>
        /// Returns true if a value exists for the given key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<bool> Contains(string key);
    }
}
