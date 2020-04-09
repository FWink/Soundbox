using LiteDB;
using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Soundbox.Test
{
    [TestClass]
    public class LiteDbDatabaseTest : DatabaseTest<LiteDbDatabaseProvider>
    {
        protected override LiteDbDatabaseProvider OpenDatabase()
        {
            return ServiceProvider.GetService(typeof(LiteDbDatabaseProvider)) as LiteDbDatabaseProvider;
        }

        protected override bool CrashDatabase()
        {
            //time for some reflection. based on LiteDB 5.0.5
            var liteDatabase = Database.GetType().GetField("Database", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(Database) as LiteDatabase;
            var engine = liteDatabase.GetType().GetField("_engine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(liteDatabase) as LiteEngine;

            var disk = engine.GetType().GetField("_disk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(engine) as IDisposable;
            var sortDisk = engine.GetType().GetField("_sortDisk", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(engine) as IDisposable;

            disk?.Dispose();
            sortDisk?.Dispose();

            return true;
        }
    }
}
