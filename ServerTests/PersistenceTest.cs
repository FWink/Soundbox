using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Soundbox.Test
{
    /// <summary>
    /// Generic base class for persistence testing (e.g. databases...)
    /// </summary>
    public abstract class PersistenceTest<Db> : IDisposable
    {
        #region "Database Test Framework"
        /// <summary>
        /// The currently active database.
        /// </summary>
        /// <seealso cref="GetDatabase"/>
        protected Db Database;

        protected IServiceProvider ServiceProvider;

        protected PersistenceTest()
        {
            ServiceProvider = new TestServiceProvider();
        }

        /// <summary>
        /// Returns a newly opened database (via <see cref="OpenDatabase"/>).
        /// Should be called in the constructor or [TestInitialize] of implementing classes to populate <see cref="Database"/>.<br/>
        /// Can be used to close and re-open a database.
        /// </summary>
        /// <returns></returns>
        protected Db GetDatabase()
        {
            DisposeDatabase();
            Database = OpenDatabase();

            return Database;
        }

        /// <summary>
        /// Simulates a database crash via <see cref="CrashDatabase"/> and then opens a new instance via <see cref="GetDatabase"/>.
        /// </summary>
        /// <returns>
        /// False: Crashing the database is not supported. Tests should abort via <see cref="Assert.Inconclusive"/>
        /// </returns>
        protected bool ReopenDatabaseCrash()
        {
            if (!CrashDatabase())
                return false;

            //re-open
            Database = default;
            GetDatabase();

            return true;
        }

        /// <summary>
        /// Opens a database. Must not clear the database beforehand, but instead just opens an existing database or creates a new one.
        /// </summary>
        /// <returns></returns>
        protected abstract Db OpenDatabase();

        /// <summary>
        /// Disposes the <see cref="Database"/> created in <see cref="OpenDatabase"/>.
        /// </summary>
        protected virtual void DisposeDatabase()
        {
            if (Database is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Simulates a process crash by closing the database's internal files without properly disposing everything.<br/>
        /// </summary>
        /// <returns>
        /// True if the operation is supported and succeeded.
        /// </returns>
        protected abstract bool CrashDatabase();

        public void Dispose()
        {
            DisposeDatabase();
            if(ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
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

            //create a new database per test
            GetDatabase();
        }

        [TestCleanup]
        public virtual void TestTearDown()
        {
            DisposeDatabase();
        }

        #endregion
    }
}
