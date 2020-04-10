using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    [TestClass]
    public class LiteDbPreferencesTest : PreferencesTest<LiteDbPreferencesProvider<bool>, bool>
    {
        protected override bool CrashDatabase()
        {
            //TODO
            return false;
        }

        protected override LiteDbPreferencesProvider<bool> OpenDatabase()
        {
            return ServiceProvider.GetService(typeof(LiteDbPreferencesProvider<bool>)) as LiteDbPreferencesProvider<bool>;
        }
    }
}
