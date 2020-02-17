using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public abstract class SoundboxContext
    {
        /// <summary>
        /// Returns the absolute directory to which all <see cref="SoundboxFile.AbsoluteFileName"/>s are relative.
        /// I.e. this path + <see cref="SoundboxFile.AbsoluteFileName"/> are an actually absolute file path in the executing environment.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="GetAbsoluteFileName(SoundboxFile)"/>
        public abstract string GetSoundsRootDirectory();

        /// <summary>
        /// Returns the absolute path of the given file in the local file system.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string GetAbsoluteFileName(SoundboxFile file)
        {
            return GetSoundsRootDirectory() + file.AbsoluteFileName;
        }
    }
}
