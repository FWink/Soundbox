using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Provides a persistent database storage for the Soundbox's file structure.
    /// </summary>
    public interface IDatabaseProvider
    {
        /// <summary>
        /// Loads the entire file tree and returns the root directory.
        /// </summary>
        /// <returns>
        /// Null if the database is empty
        /// </returns>
        Task<SoundboxDirectory> Get();

        /// <summary>
        /// Inserts the given file or directory into the database.<br/>
        /// The file's <see cref="SoundboxFile.ParentDirectory"/> and <see cref="SoundboxDirectory.Children"/>
        /// are only inserted as references.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task Insert(SoundboxFile file);

        /// <summary>
        /// Updates the given file or directory (identified by its unique <see cref="SoundboxFile.ID/>).<br/>
        /// The file's <see cref="SoundboxFile.ParentDirectory"/> and <see cref="SoundboxDirectory.Children"/>
        /// themselves are not updaed, only the references on the given file are updated if required.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task Update(SoundboxFile file);

        /// <summary>
        /// Deletes the given file or directory (identified by its unique <see cref="SoundboxFile.ID/>).<br/>
        /// If <paramref name="file"/> is a directory, then all its content is deleted recursively.<br/>
        /// May or may not remove the file from the parent's <see cref="SoundboxDirectory.Children"/>.
        /// The caller should call <see cref="Update(SoundboxFile)"/> on the directory.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task Delete(SoundboxFile file);
    }
}
