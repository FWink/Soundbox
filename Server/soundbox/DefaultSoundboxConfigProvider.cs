using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Default implementation of <see cref="ISoundboxConfigProvider"/> that wraps <see cref="IConfiguration"/>.
    /// </summary>
    public class DefaultSoundboxConfigProvider : ISoundboxConfigProvider
    {
        /// <summary>
        /// Absolute path of the soundbox's root directory.
        /// </summary>
        protected string BaseDirectory;

        public DefaultSoundboxConfigProvider(IConfiguration config)
        {
            //TODO paths from config
            this.BaseDirectory = "~/.soundbox/";
        }

        public string GetRootDirectory()
        {
            return this.BaseDirectory;
        }
    }
}
