using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Provides <see cref="SoundMetaData"/> for a <see cref="Sound"/>.
    /// </summary>
    public interface IMetaDataProvider
    {
        /// <summary>
        /// Reads the sound file's meta data from disk and returns the parsed meta data (as much as this provider is capable).
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sound"></param>
        /// <returns></returns>
        Task<SoundMetaData> GetMetaData(string filePath);

        /// <summary>
        /// Reads the sound file from <see cref="SoundboxContext.GetAbsoluteFileName(SoundboxFile)"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sound"></param>
        /// <returns></returns>
        /// <seealso cref="GetMetaData(string)"/>
        Task<SoundMetaData> GetMetaData(SoundboxContext context, Sound sound)
        {
            return GetMetaData(context.GetAbsoluteFileName(sound));
        }
    }
}
