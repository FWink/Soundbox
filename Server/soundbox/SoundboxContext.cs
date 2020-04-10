using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public abstract class SoundboxContext
    {
        /// <summary>
        /// Returns the absolute directory to which all <see cref="SoundboxNode.AbsoluteFileName"/>s are relative.
        /// I.e. this path + <see cref="SoundboxNode.AbsoluteFileName"/> are an actually absolute file path in the executing environment.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="GetAbsoluteFileName(SoundboxNode)"/>
        public abstract string GetSoundsRootDirectory();

        /// <summary>
        /// Returns the absolute path of the given file in the local file system.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public string GetAbsoluteFileName(SoundboxNode file)
        {
            return GetSoundsRootDirectory() + file.AbsoluteFileName;
        }
    }
}
