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
    }
}
