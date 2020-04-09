using System;
using System.Collections.Generic;
using System.Text;

namespace Soundbox.Test
{
    public class TestConfigProvider : ISoundboxConfigProvider
    {
        public string GetRootDirectory()
        {
            //TODO tests
            return "test_files/";
        }
    }
}
