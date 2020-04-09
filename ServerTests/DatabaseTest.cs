using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Soundbox.Test
{
    public abstract class DatabaseTest<Db> : IDisposable where Db : IDatabaseProvider
    {
        #region "Database Test Framework"
        /// <summary>
        /// The currently active database.
        /// </summary>
        /// <seealso cref="GetDatabase"/>
        protected Db Database;

        protected IServiceProvider ServiceProvider;

        protected DatabaseTest()
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
        protected void DisposeDatabase()
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
            catch (DirectoryNotFoundException ex)
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

        #region "Utilities"

        /// <summary>
        /// Compares two files for equality by comparing all their fields and properties.<br/>
        /// Performs only a shallow comparison for <see cref="SoundboxDirectory.Children"/> and <see cref="SoundboxFile.ParentDirectory"/>
        /// unless otherwise specified via <paramref name="deepCompareParents"/> and <paramref name="deepCompareChildren"/>.
        /// </summary>
        /// <param name="file1"></param>
        /// <param name="file2"></param>
        /// <param name="compareDistinct">
        /// True: Returns false if the given files are the same object (<see cref="Object.ReferenceEquals(object, object)"/>).
        /// </param>
        /// <returns></returns>
        protected bool Compare(SoundboxFile file1, SoundboxFile file2, bool compareDistinct = false, bool deepCompareParents = false, bool deepCompareChildren = false)
        {
            if (Object.ReferenceEquals(file1, file2))
            {
                if (compareDistinct && file1 != null)
                    return false;
                return true;
            }
            if (file1 != file2)
                return false;
            if ((file1 is SoundboxDirectory) != (file2 is SoundboxDirectory))
                return false;
            if ((file1 is Sound) != (file2 is Sound))
                return false;

            if (file1.Name != file2.Name)
                return false;
            if (file1.FileName != file2.FileName)
                return false;
            if (file1.AbsoluteFileName != file2.AbsoluteFileName)
                return false;
            if (file1.IconUrl != file2.IconUrl)
                return false;
            if (file1.ParentDirectory != file2.ParentDirectory)
                return false;
            if (deepCompareParents && !Compare(file1.ParentDirectory, file2.ParentDirectory, compareDistinct: compareDistinct, deepCompareParents: true))
                return false;
            if (!file1.Tags.CollectionsEqual(file2.Tags))
                return false;

            if ((file1 is Sound sound1) && (file2 is Sound sound2))
            {
                if (sound1.Length != sound2.Length)
                    return false;
            }

            if ((file1 is SoundboxDirectory dir1) && (file2 is SoundboxDirectory dir2))
            {
                if (!dir2.Children.CollectionsEqual(dir2.Children))
                    return false;

                if(deepCompareChildren)
                {
                    var children2 = new List<SoundboxFile>(dir2.Children);
                    foreach(var child1 in dir1.Children)
                    {
                        if (children2.Find(child2 => Compare(child1, child2, compareDistinct: compareDistinct, deepCompareParents: true, deepCompareChildren: true)) == null)
                            return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region "Tests"

        #region "Insert/Get"

        /// <summary>
        /// Prepares the database for <see cref="TestInsertDirectoryGet"/> and returns the root directory that has been inserted.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> TestInsertDirectoryGet_Prepare()
        {
            SoundboxDirectory directoryRoot = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Name",
                FileName = "FileName",
                AbsoluteFileName = "AbsoluteFileName",
                IconUrl = "IconUrl",
                Watermark = Guid.NewGuid(),
                Tags = new string[] { "1", "2", "3" }
            };

            //insert
            await Database.Insert(directoryRoot);

            return directoryRoot;
        }

        /// <summary>
        /// Inserts a single directory (the root directory) via <see cref="IDatabaseProvider.Insert(SoundboxFile)"/> and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.
        /// </summary>
        [TestMethod]
        public async Task TestInsertDirectoryGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryGet_Prepare();

            //get
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true));
        }

        /// <summary>
        /// Prepares the database for <see cref="TestInsertDirectorySoundGet"/> and returns the root directory.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> TestInsertDirectorySoundGet_Prepare()
        {
            SoundboxDirectory directoryRoot = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Directory"
            };
            Sound sound = new Sound()
            {
                ID = Guid.NewGuid(),
                Name = "Name",
                FileName = "FileName",
                AbsoluteFileName = "AbsoluteFileName",
                IconUrl = "IconUrl",
                Tags = new string[] { "1", "2", "3" },
                Length = 17
            };

            directoryRoot.AddChild(sound);

            //insert
            await Database.Insert(directoryRoot);
            await Database.Insert(sound);

            return directoryRoot;
        }

        /// <summary>
        /// Inserts a directory (root) and a sound and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundGet_Prepare();

            //get
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Prepares the database for <see cref="TestInsertDirectoryDirectoryGet"/> and returns the root directory.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> TestInsertDirectoryDirectoryGet_Prepare()
        {
            SoundboxDirectory directoryRoot = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Directory"
            };
            SoundboxDirectory directoryChild = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Name",
                FileName = "FileName",
                AbsoluteFileName = "AbsoluteFileName",
                IconUrl = "IconUrl",
                Tags = new string[] { "1", "2", "3" }
            };

            directoryRoot.AddChild(directoryChild);

            //insert
            await Database.Insert(directoryRoot);
            await Database.Insert(directoryChild);

            return directoryRoot;
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectorySoundGet"/> but inserts a child directory instead.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectoryGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectoryGet_Prepare();

            //get
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Prepares the database for <see cref="TestInsertDirectoryDirectorySoundGet"/> and returns the root directory.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> TestInsertDirectoryDirectorySoundGet_Prepare()
        {
            SoundboxDirectory directoryRoot = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Directory"
            };
            SoundboxDirectory directoryChild = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "DirectoryChild",
            };
            Sound sound = new Sound()
            {
                ID = Guid.NewGuid(),
                Name = "Name",
                FileName = "FileName",
                AbsoluteFileName = "AbsoluteFileName",
                IconUrl = "IconUrl",
                Tags = new string[] { "1", "2", "3" },
                Length = 17
            };

            directoryRoot.AddChild(directoryChild);
            directoryChild.AddChild(sound);

            //insert
            await Database.Insert(directoryRoot);
            await Database.Insert(directoryChild);
            await Database.Insert(sound);

            return directoryRoot;
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the child directory.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectorySoundGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectorySoundGet_Prepare();

            //get
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Prepares the database for <see cref="TestInsertDirectorySoundDirectoryGet"/> and returns the root directory.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> TestInsertDirectorySoundDirectoryGet_Prepare()
        {
            SoundboxDirectory directoryRoot = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "Directory"
            };
            SoundboxDirectory directoryChild = new SoundboxDirectory()
            {
                ID = Guid.NewGuid(),
                Name = "DirectoryChild",
            };
            Sound sound = new Sound()
            {
                ID = Guid.NewGuid(),
                Name = "Name",
                FileName = "FileName",
                AbsoluteFileName = "AbsoluteFileName",
                IconUrl = "IconUrl",
                Tags = new string[] { "1", "2", "3" },
                Length = 17
            };

            directoryRoot.AddChild(directoryChild);
            directoryRoot.AddChild(sound);

            //insert
            await Database.Insert(directoryRoot);
            await Database.Insert(directoryChild);
            await Database.Insert(sound);

            return directoryRoot;
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the root directory (sibling to the directory).
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundDirectoryGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundDirectoryGet_Prepare();

            //get
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        #endregion

        #region "Insert/Reopen/Get"

        /// <summary>
        /// Inserts a single directory (the root directory) via <see cref="IDatabaseProvider.Insert(SoundboxFile)"/> and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.<br/>
        /// Closes and re-opens the database before fetching.
        /// </summary>
        [TestMethod]
        public async Task TestInsertDirectoryReopenGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryGet_Prepare();

            //reopen and get
            GetDatabase();
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true));
        }

        /// <summary>
        /// Inserts a directory (root) and a sound and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.<br/>
        /// Closes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundReopenGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundGet_Prepare();

            //reopen and get
            GetDatabase();
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectorySoundGet"/> but inserts a child directory instead.<br/>
        /// Closes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectoryReopenGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectoryGet_Prepare();

            //reopen and get
            GetDatabase();
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the child directory.<br/>
        /// Closes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectorySoundReopenGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectorySoundGet_Prepare();

            //reopen and get
            GetDatabase();
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the root directory (sibling to the directory).<br/>
        /// Closes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundDirectoryReopenGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundDirectoryGet_Prepare();

            //reopen and get
            GetDatabase();
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        #endregion

        #region "Insert/Crash/Get"

        /// <summary>
        /// Inserts a single directory (the root directory) via <see cref="IDatabaseProvider.Insert(SoundboxFile)"/> and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.<br/>
        /// Crashes and re-opens the database before fetching.
        /// </summary>
        [TestMethod]
        public async Task TestInsertDirectoryCrashGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryGet_Prepare();

            //crash and get
            if(!ReopenDatabaseCrash())
            {
                Assert.Inconclusive("Database provider cannot crash on purpose");
                return;
            }
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true));
        }

        /// <summary>
        /// Inserts a directory (root) and a sound and fetches it afterwards with <see cref="IDatabaseProvider.Get"/>.<br/>
        /// Crashes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundCrashGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundGet_Prepare();

            //crash and get
            if (!ReopenDatabaseCrash())
            {
                Assert.Inconclusive("Database provider cannot crash on purpose");
                return;
            }
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectorySoundGet"/> but inserts a child directory instead.<br/>
        /// Crashes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectoryCrashGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectoryGet_Prepare();

            //crash and get
            if (!ReopenDatabaseCrash())
            {
                Assert.Inconclusive("Database provider cannot crash on purpose");
                return;
            }
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the child directory.<br/>
        /// Crashes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectoryDirectorySoundCrashGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectoryDirectorySoundGet_Prepare();

            //crash and get
            if (!ReopenDatabaseCrash())
            {
                Assert.Inconclusive("Database provider cannot crash on purpose");
                return;
            }
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        /// <summary>
        /// Like <see cref="TestInsertDirectoryDirectoryGet"/> but adds a sound to the root directory (sibling to the directory).<br/>
        /// Crashes and re-opens the database before fetching.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestInsertDirectorySoundDirectoryCrashGet()
        {
            SoundboxDirectory directoryRoot = await TestInsertDirectorySoundDirectoryGet_Prepare();

            //crash and get
            if (!ReopenDatabaseCrash())
            {
                Assert.Inconclusive("Database provider cannot crash on purpose");
                return;
            }
            var fromDb = await Database.Get();

            Assert.IsTrue(Compare(directoryRoot, fromDb, compareDistinct: true, deepCompareParents: true, deepCompareChildren: true));
        }

        #endregion

        #endregion
    }
}
