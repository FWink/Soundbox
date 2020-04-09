using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Abstraction layer (for example around <see cref="IConfiguration"/>) that provides Soundbox specific configurations.
    /// </summary>
    public interface ISoundboxConfigProvider
    {
        /// <summary>
        /// Returns the Soundbox's root directory where all persistent data is stored (files, database...)
        /// </summary>
        /// <returns></returns>
        string GetRootDirectory();
    }
}
