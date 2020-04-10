using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Singleton that provides a <see cref="ILiteDatabase"/> to multiple instances of <see cref="LiteDbPreferencesProvider{T}"/>
    /// </summary>
    public class LiteDbPreferencesDatabaseProvider : IDisposable
    {
        protected readonly LiteDatabase Database;

        public LiteDbPreferencesDatabaseProvider(ISoundboxConfigProvider config)
        {
            //open the database file
            var databaseDirectory = config.GetRootDirectory() + "database/";
            Directory.CreateDirectory(databaseDirectory);
            Database = new LiteDatabase(databaseDirectory + "lite_preferences.db");
        }

        /// <summary>
        /// Returns a shared LiteDB instance
        /// </summary>
        /// <returns></returns>
        public ILiteDatabase GetDatabase()
        {
            return Database;
        }

        public void Dispose()
        {
            Database?.Dispose();
        }
    }
}
