using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Soundbox.Test
{
    /// <summary>
    /// Base class to test <see cref="IPreferencesProvider{T}"/> with different value types.
    /// </summary>
    /// <typeparam name="Pref"></typeparam>
    /// <typeparam name="Val"></typeparam>
    public abstract class PreferencesValueTest<Pref, Val> : IDisposable where Pref : IPreferencesProvider<Val>
    {
        #region "Preferences Test Framework"
        /// <summary>
        /// The currently active preferences.
        /// </summary>
        protected Pref Preferences;

        protected IServiceProvider ServiceProvider;

        protected PreferencesValueTest()
        {
            ServiceProvider = new TestServiceProvider();
        }

        [TestInitialize]
        public virtual void TestSetup()
        {
            var config = ServiceProvider.GetService(typeof(ISoundboxConfigProvider)) as ISoundboxConfigProvider;
            //delete all files before each test
            try
            {
                System.IO.Directory.Delete(config.GetRootDirectory(), true);
            }
            catch (DirectoryNotFoundException)
            {
                //ignore
            }

            //create new preferences per test
            Preferences = (Pref)ServiceProvider.GetService(typeof(Pref));
        }

        public void Dispose()
        {
            if(Preferences is IDisposable prefDispose)
            {
                prefDispose.Dispose();
            }
            if(ServiceProvider is IDisposable servicesDispose)
            {
                servicesDispose.Dispose();
            }
        }
        #endregion
    }
}
