using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Soundbox.Test
{
    /// <summary>
    /// Tests <see cref="IPreferencesProvider{T}"/> with double values.
    /// </summary>
    public abstract class PreferencesDoubleTest<Pref> : PreferencesValueTest<Pref, double> where Pref : IPreferencesProvider<double>
    {
        /// <summary>
        /// Tests <see cref="IPreferencesProvider{T}.Set(string, T)"/> and <see cref="IPreferencesProvider{T}.Get(string)"/>
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        [DataRow(1)]
        [DataRow(2147483648)]
        [DataRow(-2147483648)]
        [DataRow(4294967296)]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        [DataRow(double.NegativeInfinity)]
        [DataRow(double.Epsilon)]
        public async Task TestInsertGet(double value)
        {
            string key = "key";
            await Preferences.Set(key, value);
            Assert.AreEqual(value, await Preferences.Get(key));
        }

        /// <summary>
        /// Calls <see cref="IPreferencesProvider{T}.Set(string, T)"/> multiple times. <see cref="IPreferencesProvider{T}.Get(string)"/> must return the last inserted value.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestMultiInsertGet()
        {
            string key = "key";

            await Preferences.Set(key, 1);
            Assert.AreEqual(1, await Preferences.Get(key));
            await Preferences.Set(key, 2);
            Assert.AreEqual(2, await Preferences.Get(key));
            await Preferences.Set(key, 3);
            Assert.AreEqual(3, await Preferences.Get(key));
            await Preferences.Set(key, 4);
            Assert.AreEqual(4, await Preferences.Get(key));
        }
    }
}
