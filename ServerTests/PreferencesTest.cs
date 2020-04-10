using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Soundbox.Test
{
    public abstract class PreferencesTest<Pref, Val> : PersistenceTest<Pref> where Pref : IPreferencesProvider<Val>
    {
        #region "Default tests"

        /// <summary>
        /// Calls <see cref="IPreferencesProvider{T}.Set(string, T)"/> and <see cref="IPreferencesProvider{T}.Contains(string)"/>
        /// </summary>
        [TestMethod]
        public async Task TestDefaultInsertFind()
        {
            string key = "key";

            Assert.IsFalse(await Database.Contains(key));
            await Database.Set(key, default);
            Assert.IsTrue(await Database.Contains(key));
        }

        /// <summary>
        /// Calls <see cref="IPreferencesProvider{T}.Set(string, T)"/>, <see cref="IPreferencesProvider{T}.Delete(string)"/> and <see cref="IPreferencesProvider{T}.Contains(string)"/> to verify the deletion.
        /// </summary>
        [TestMethod]
        public async Task TestDefaultInsertDelete()
        {
            string key = "key";

            Assert.IsFalse(await Database.Contains(key));
            await Database.Set(key, default);
            Assert.IsTrue(await Database.Contains(key));
            await Database.Delete(key);
            Assert.IsFalse(await Database.Contains(key));
        }

        /// <summary>
        /// Calls <see cref="IPreferencesProvider{T}.Set(string, T)"/> and <see cref="IPreferencesProvider{T}.Get(string)"/>.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestDefaultInsertGet()
        {
            string key = "key";
            Val value = default;

            Assert.IsFalse(await Database.Contains(key));
            await Database.Set(key, value);
            Assert.IsTrue(await Database.Contains(key));

            Assert.AreEqual(value, await Database.Get(key));
        }

        /// <summary>
        /// Calls <see cref="IPreferencesProvider{T}.Set(string, T)"/> multiple times but <see cref="IPreferencesProvider{T}.Delete(string)"/> only once.
        /// <see cref="IPreferencesProvider{T}.Contains(string)"/> must return false afterwards.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestDefaultMultiInsertDelete()
        {
            string key = "key";

            Assert.IsFalse(await Database.Contains(key));
            await Database.Set(key, default);
            Assert.IsTrue(await Database.Contains(key));
            await Database.Set(key, default);
            Assert.IsTrue(await Database.Contains(key));
            await Database.Set(key, default);
            Assert.IsTrue(await Database.Contains(key));
            await Database.Delete(key);
            Assert.IsFalse(await Database.Contains(key));
        }

        #endregion
    }
}
