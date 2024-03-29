﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundbox.AppSettings;
using Soundbox.Speech.Recognition;
using Soundbox.Users;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Singleton class that manages a list/tree of available sounds and handles playback.
    /// Clients are notified of the playback state etc via <see cref="SoundboxHub"/>
    /// </summary>
    public class Soundbox : SoundboxContext
    {
        protected static readonly ICollection<string> SUPPORTED_FILE_TYPES = new string[]
        {
            "mp3",
            "wav",
            "ogg",
            "aac",
            "flac"
        };

        protected ILogger Logger;

        protected IHubContext<SoundboxHub, ISoundboxClient> HubContext;

        protected IServiceProvider ServiceProvider;

        protected SoundboxAppSettings AppSettings;

        /// <summary>
        /// Absolute path of the soundbox's root directory.
        /// This is not however the root directory of <see cref="SoundboxNode"/>s
        /// (see <see cref="SoundsRootDirectory"/>).
        /// </summary>
        protected string BaseDirectory;

        /// <summary>
        /// The root directory for the entire <see cref="SoundboxNode"/> tree.
        /// I.e. all <see cref="SoundboxFile.AbsoluteFileName"/>s are given relative to this path.
        /// </summary>
        protected string SoundsRootDirectory;

        /// <summary>
        /// Persistent database where we don't store the files themselves, but the meta data (name, tags etc)
        /// </summary>
        protected IDatabaseProvider Database;

        public Soundbox(
            IServiceProvider serviceProvider,
            IHubContext<SoundboxHub, ISoundboxClient> hubContext,
            ISoundboxConfigProvider config,
            IDatabaseProvider database,
            ISpeechRecognitionServiceProvider speechRecognitionServiceProvider,
            ILogger<Soundbox> logger,
            IOptions<SoundboxAppSettings> appSettings)
        {
            this.Logger = logger;
            logger.LogInformation("Soundbox is starting");

            this.AppSettings = appSettings.Value ?? new SoundboxAppSettings();

            this.ServiceProvider = serviceProvider;
            this.HubContext = hubContext;

            this.BaseDirectory = config.GetRootDirectory();
            this.SoundsRootDirectory = this.BaseDirectory + "sounds/";
            Directory.CreateDirectory(this.SoundsRootDirectory);

            this.Database = database;

            //initialize our file tree
            var taskSoundsRoot = LoadRoot();
            taskSoundsRoot.Wait();
            this.SoundsRoot = taskSoundsRoot.Result;

            BuildNodeCache(this.SoundsRoot);

            SpeechRecognition_Setup(speechRecognitionServiceProvider);
        }

        protected ISoundboxClient GetHub()
        {
            return HubContext.Clients.All;
        }

        #region "Sounds Database"

        /// <summary>
        /// Lock used when accessing the file structure or our (in-process) database or our in-process cache (<see cref="GetSoundsTree"/>).
        /// </summary>
        protected ReaderWriterLockSlim DatabaseLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        #region "Sounds Cache"
        /// <summary>
        /// Root directory of our file tree.
        /// </summary>
        protected SoundboxDirectory SoundsRoot;

        /// <summary>
        /// Lookup table for all nodes in our sounds tree.
        /// </summary>
        /// <remarks>
        /// Currently we cache all nodes here. At some later point we can decide to cache only a limited number of nodes
        /// while loading nodes on-demand from the database.
        /// </remarks>
        protected readonly IDictionary<Guid, SoundboxNode> NodesCache = new Dictionary<Guid, SoundboxNode>();
        #endregion

        /// <summary>
        /// Loads the root directory and thuse the entire file tree from our persistent database.
        /// If the database is empty then a newly created empty directory is returned.
        /// </summary>
        /// <returns></returns>
        protected async Task<SoundboxDirectory> LoadRoot()
        {
            var root = await Database.Get();

            if (root == null)
            {
                //database is empty => create new root directory
                root = new SoundboxDirectory();
                root.ID = Guid.NewGuid();
                root.Watermark = Guid.NewGuid();

                await Database.Insert(root);
            }

            return root;
        }

        /// <summary>
        /// Recursively adds the directory, all nodes in the directory and its descendant nodes to <see cref="NodesCache"/>.
        /// </summary>
        /// <param name="directory"></param>
        protected void BuildNodeCache(SoundboxDirectory directory)
        {
            NodesCache[directory.ID] = directory;
            foreach (var child in directory.Children)
            {
                NodesCache[child.ID] = child;
                if (child is SoundboxDirectory childDirectory)
                {
                    BuildNodeCache(childDirectory);
                }
            }
        }

        /// <summary>
        /// Returns the root directory of the <see cref="SoundboxNode"/> tree.
        /// </summary>
        /// <returns></returns>
        protected SoundboxDirectory GetRootDirectory()
        {
            return SoundsRoot;
        }

        /// <summary>
        /// Returns the given directory with its content from our database. Pass null to get the root directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        //TODO we've got a bit of a race condition here: internal access is synchronized (mostly, see usage of GetCleanFile).
        // however we can get into trouble when returning files either via public getters or via client events: files can change after they have been returned here, so while the serializer is running.
        // to fully fix this we'd have to completely copy the entire sound tree whenever something changes. returning copies only would mostly work; we'd have to make sure that internally everything is synchronized
        public Task<SoundboxDirectory> GetDirectory(SoundboxDirectory directory = null)
        {
            if (directory == null)
                return Task.FromResult(GetRootDirectory());
            return Task.FromResult(GetCleanFile(directory));
        }

        /// <summary>
        /// Sets the given watermark on all ancestors of a file (and the file itself if it represents a directory). This is required when the given file changed.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="watermark"></param>
        protected void SetWatermark(SoundboxNode file, Guid watermark)
        {
            if (file is SoundboxDirectory)
                SetWatermark(file as SoundboxDirectory, watermark);
            else
                SetWatermark(file.ParentDirectory, watermark);
        }

        /// <summary>
        /// Sets the given watermark on a directory and all of its ancestors. This is required whenever this directory or its content is changed.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="watermark"></param>
        protected void SetWatermark(SoundboxDirectory directory, Guid watermark)
        {
            do
            {
                directory.Watermark = watermark;
                //TODO async
                Database.Update(directory);
                directory = directory.ParentDirectory;
            } while (directory != null);
        }

        /// <summary>
        /// Returns the current root-level watermark of the file tree.
        /// </summary>
        /// <returns></returns>
        protected Guid GetRootWatermark()
        {
            return GetRootDirectory().Watermark;
        }

        #region "File management"

        public override string GetSoundsRootDirectory()
        {
            return SoundsRootDirectory;
        }

        /// <summary>
        /// Returns the directory where various temporary files/directories may be created.
        /// </summary>
        /// <returns></returns>
        protected string GetTempDirectory()
        {
            return BaseDirectory + "tmp/";
        }

        /// <summary>
        /// Normalizes the given file name for storage on the local file system:<list type="bullet">
        /// <item>
        /// lower case
        /// </item>
        /// <item>
        /// ASCII only
        /// </item>
        /// <item>
        /// no white spaces
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string NormalizeFileName(string fileName)
        {
            //lower case
            fileName = fileName.ToLowerInvariant();
            //replace some common unicode characters
            fileName = Regex.Replace(fileName, "[äáàâ]", "a");
            fileName = Regex.Replace(fileName, "[öóòô]", "o");
            fileName = Regex.Replace(fileName, "[üúùû]", "u");
            fileName = Regex.Replace(fileName, "[ß]", "ss");
            //purge non-ASCII characters and characters invalid for files
            fileName = Regex.Replace(fileName, @"[^a-z0-9.\-_]", "_");

            if (IsFileNameReserved(fileName))
            {
                fileName = "_" + fileName;
            }

            return fileName;
        }

        /// <summary>
        /// Checks if the given (lower case) file name is a reserved name on common operating systems
        /// (mostly Windows, there are several reserved file names).
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool IsFileNameReserved(string fileName)
        {
            fileName = GetFileNamePure(fileName);
            if (fileName == null)
                return false;

            return fileName == "con" ||
                fileName == "prn" ||
                fileName == "aux" ||
                fileName == "nul" ||
                Regex.IsMatch(fileName, @"^(com|lpt)\d$");
        }

        /// <summary>
        /// Returns the given file without any extension. This is used by <see cref="IsFileNameReserved(string)"/>
        /// to check for reserved file names.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string GetFileNamePure(string fileName)
        {
            if (fileName == null)
                return null;
            var match = Regex.Match(fileName, @"^([^.]*)\.");
            if (match.Success)
                return match.Groups[1].Value;
            return fileName;
        }

        /// <summary>
        /// Returns the given file's type (extension) if any.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected string GetFileType(string fileName)
        {
            if (fileName == null)
                return null;
            var match = Regex.Match(fileName, @"\.([^.]+)$");
            if (match.Success)
                return match.Groups[1].Value;
            return null;
        }

        #endregion

        #region "Upload"

        /// <summary>
        /// Uploads a new sound and adds it to the given directory.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="sound">
        /// Information used when adding the new sound:<list type="bullet">
        /// <item><see cref="SoundboxNode.Name"/></item>
        /// <item><see cref="SoundboxFile.FileName"/></item>
        /// <item><see cref="SoundboxNode.Tags"/></item>
        /// <item><see cref="Sound.VoiceActivation"/></item>
        /// </list>
        /// </param>
        /// <param name="directory">
        /// Null: the root directory is used instead.
        /// </param>
        /// <returns></returns>
        public async Task<FileResult> UploadSound(Stream bytes, Sound sound, SoundboxDirectory directory)
        {
            if(sound == null || bytes == null)
            {
                //error
                return new FileResult(BaseResultStatus.INVALID_PARAMETER);
            }
            if(!CheckUploadFileName(sound.FileName))
            {
                if(GetFileType(sound.FileName) != null && !CheckUploadFileType(sound.FileName))
                {
                    //file type is not supported
                    return new FileResult(FileResultStatus.ILLEGAL_FILE_TYPE);
                }
                return new FileResult(FileResultStatus.INVALID_FILE_NAME);
            }
            if(!CheckUploadDisplayName(sound.Name))
            {
                //error
                return new FileResult(FileResultStatus.INVALID_FILE_NAME);
            }

            if (directory == null)
                directory = GetRootDirectory();
            else
            {
                directory = GetCleanFile(directory);
                if(directory == null)
                {
                    //does not exist
                    return new FileResult(FileResultStatus.FILE_DOES_NOT_EXIST);
                }
            }

            //TODO check display name unique in parent

            //we store only a few select properties of the given sound
            Sound soundClean = new Sound();
            soundClean.ID = Guid.NewGuid();
            soundClean.Name = sound.Name;
            soundClean.FileName = sound.FileName;
            soundClean.Tags = sound.Tags;
            soundClean.VoiceActivation = FilterUploadVoiceActivationSettings(sound.VoiceActivation);
            soundClean.ParentDirectory = directory;

            sound = soundClean;
            MakeSoundFileName(sound);

            ResultStatus status = BaseResultStatus.INTERNAL_SERVER_ERROR;

            //TODO: write into temp file, lock database etc, copy file, add to directory object, save database, update clients, unlock
            //write into temp file
            string tempFile = GetUploadTempFile();
            try
            {
                try
                {
                    using (var output = File.OpenWrite(tempFile))
                    {
                        await bytes.CopyToAsync(output);
                    }
                }
                catch
                {
                    status = FileResultStatus.IO_ERROR;
                    throw;
                }

                //read meta data from temp file (i.e. before we enter the database lock)
                var metaDataProvider = ServiceProvider.GetService(typeof(IMetaDataProvider)) as IMetaDataProvider;
                if(metaDataProvider != null)
                {
                    sound.MetaData = await metaDataProvider.GetMetaData(tempFile);
                }

                try
                {
                    DatabaseLock.EnterWriteLock();

                    //move the file to the target location
                    try
                    {
                        File.Move(tempFile, GetAbsoluteFileName(sound));
                    }
                    catch
                    {
                        status = FileResultStatus.IO_ERROR;
                        //delete the target file
                        try
                        {
                            File.Delete(GetAbsoluteFileName(sound));
                        }
                        catch { }

                        throw;
                    }

                    return UploadOnNewFile(sound, directory);
                }
                finally
                {
                    DatabaseLock.ExitWriteLock();
                }
            }
            catch(Exception ex)
            {
                Log(ex);
            }
            finally
            {
                //delete the temp file should it still exist
                try
                {
                    File.Delete(tempFile);
                }
                catch { }
            }

            return new FileResult(status);
        }

        /// <summary>
        /// Adds the given new file to our in-memory data structures and to our database. Updates all clients on success.
        /// </summary>
        /// <param name="newFile"></param>
        /// <param name="parent"></param>
        private FileResult UploadOnNewFile(SoundboxNode newFile, SoundboxDirectory parent)
        {
            try
            {
                DatabaseLock.EnterWriteLock();

                //save previous watermark for event
                Guid previousWatermark = GetRootWatermark();
                Guid newWatermark = Guid.NewGuid();

                //add to cache
                parent.AddChild(newFile);
                NodesCache[newFile.ID] = newFile;
                //add to database
                //TODO async
                Database.Insert(newFile);

                //update cache and database watermarks (this will call Update for parent)
                SetWatermark(newFile, newWatermark);

                //update our clients
                GetHub().OnFileEvent(new SoundboxFileChangeEvent()
                {
                    Event = SoundboxFileChangeEvent.Type.ADDED,
                    File = FlattenForEvent(newFile),
                    PreviousWatermark = previousWatermark
                });

                if (newFile is Sound sound)
                {
                    SpeechRecognition_OnSoundChanged(sound, null);
                }

                return new FileResult(BaseResultStatus.OK, newFile, previousWatermark);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Creates file path for the given sound where it should be stored on disk. The file name is guaranteed to be unique in its directory.<br/>
        /// <see cref="SoundboxFile.FileName"/> and <see cref="SoundboxFile.AbsoluteFileName"/> are updated on the given object.
        /// </summary>
        /// <param name="sound"></param>
        protected void MakeSoundFileName(Sound sound)
        {
            //make a unique name
            sound.FileName = string.Format("{0}_{1}.{2}", NormalizeFileName(sound.Name), sound.ID.ToString(), GetFileType(sound.FileName));
            //sound is in root directory
            //TODO at some point we might want to organize our sounds in different directories (for performance reasons mostly, when opening the sound directory in explorer)
            sound.AbsoluteFileName = sound.FileName;
        }

        /// <summary>
        /// Checks if the given file name of a new sound is valid (e.g. is not empty, is of a supported file type...)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool CheckUploadFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            if (!CheckUploadFileType(fileName))
                return false;

            fileName = GetFileNamePure(fileName);
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return true;
        }

        /// <summary>
        /// Checks the given file's type: must not be empty and must be supported (<see cref="SUPPORTED_FILE_TYPES"/>).
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool CheckUploadFileType(string fileName)
        {
            string type = GetFileType(fileName);
            if (type == null)
                return false;
            type = type.ToLowerInvariant();

            return SUPPORTED_FILE_TYPES.Contains(type);
        }

        /// <summary>
        /// Checks if the given <see cref="SoundboxNode.Name"/> is valid (e.g. is not empty...)
        /// </summary>
        /// <param name="displayName"></param>
        /// <returns></returns>
        protected bool CheckUploadDisplayName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            return true;
        }

        /// <summary>
        /// Performs some simple error checking on the given voice activation settings: filters empty or double phrases and keywords.
        /// </summary>
        /// <param name="voiceActivation"></param>
        /// <returns></returns>
        protected SoundboxVoiceActivation FilterUploadVoiceActivationSettings(SoundboxVoiceActivation voiceActivation)
        {
            if (voiceActivation == null)
                return null;
            voiceActivation.SpeechTriggers = FilterUploadVoiceActivationStrings(voiceActivation.SpeechTriggers);
            voiceActivation.SpeechPhrases = FilterUploadVoiceActivationStrings(voiceActivation.SpeechPhrases);

            if (voiceActivation.SpeechTriggers.Count == 0)
                return null;
            return voiceActivation;
        }

        /// <summary>
        /// Processes either <see cref="SoundboxVoiceActivation.SpeechTriggers"/> or <see cref="SoundboxVoiceActivation.SpeechPhrases"/>
        /// for <see cref="FilterUploadVoiceActivationSettings(SoundboxVoiceActivation)"/>: filters empty or double strings.
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        protected ICollection<string> FilterUploadVoiceActivationStrings(ICollection<string> strings)
        {
            if (strings == null)
                return new List<string>();
            return strings
                .Where(str => str != null)
                .Select(str => str.Trim())
                .Where(str => str.Length > 0)
                .ToHashSet();
        }

        /// <summary>
        /// Returns the name of a temporary file for the upload of a new sound.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="GetUploadTempDirectory"/>
        protected string GetUploadTempFile()
        {
            return GetUploadTempDirectory() + Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Returns the directory where uploaded files are buffered until the upload is complete.
        /// The directory is created if it does not exist yet.<br/>
        /// The directory and its content may be cleared completely on startup.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="GetUploadTempFile"/>
        protected string GetUploadTempDirectory()
        {
            string directory = GetTempDirectory() + "upload/";
            Directory.CreateDirectory(directory);

            return directory;
        }

        #endregion

        #region "Directories"

        /// <summary>
        /// Creates a new directory in the given parent directory.
        /// </summary>
        /// <param name="directory">
        /// Information used when adding the new sound:<list type="bullet">
        /// <item><see cref="SoundboxNode.Name"/></item>
        /// <item><see cref="SoundboxNode.Tags"/></item>
        /// </list>
        /// </param>
        /// <param name="parent">
        /// Null: root directory is assumed.
        /// </param>
        /// <returns></returns>
        public FileResult MakeDirectory(SoundboxDirectory directory, SoundboxDirectory parent)
        {
            if(directory == null)
            {
                //error
                return new FileResult(BaseResultStatus.INVALID_PARAMETER);
            }
            if(!CheckUploadDisplayName(directory.Name))
            {
                //error
                return new FileResult(FileResultStatus.INVALID_FILE_NAME);
            }

            if (parent == null)
                parent = GetRootDirectory();
            else
            {
                parent = GetCleanFile(parent);
                if(parent == null)
                {
                    //given parent does not exist => error
                    return new FileResult(FileResultStatus.FILE_DOES_NOT_EXIST);
                }
            }

            //TODO check display name unique in parent

            directory.ID = Guid.NewGuid();
            directory.Children = new List<SoundboxNode>();
            directory.ParentDirectory = parent;

            ResultStatus status = BaseResultStatus.INTERNAL_SERVER_ERROR;

            try
            {
                DatabaseLock.EnterWriteLock();

                return UploadOnNewFile(directory, parent);
            }
            catch (Exception ex)
            {
                Log(ex);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }

            return new FileResult(status);
        }

        #endregion

        #region "Edit"

        /// <summary>
        /// Edits the given file. These attributes are modified:<list type="bullet">
        /// <item><see cref="SoundboxNode.Name"/></item>
        /// <item><see cref="SoundboxNode.Tags"/></item>
        /// <item><see cref="Sound.VoiceActivation"/></item>
        /// </list>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<FileResult> Edit(SoundboxNode file)
        {
            SoundboxNode localFile = GetCleanFile(file);
            if(localFile == null)
            {
                return new FileResult(BaseResultStatus.INVALID_PARAMETER);
            }
            if(IsRootDirectory(localFile))
            {
                return new FileResult(FileResultStatus.ILLEGAL_FILE_EDIT_DENIED_ROOT);
            }
            if(!CheckUploadDisplayName(file.Name))
            {
                return new FileResult(FileResultStatus.INVALID_FILE_NAME);
            }

            //TODO check unique name in directory

            try
            {
                DatabaseLock.EnterWriteLock();

                //save previous watermark for event
                Guid previousWatermark = GetRootWatermark();
                Guid newWatermark = Guid.NewGuid();

                //make a copy of our local file for comparison
                var localCopy = localFile.CompareCopy();

                //modify our local file
                localFile.Name = file.Name;
                localFile.Tags = file.Tags;
                
                if (localFile is ISoundboxPlayable localPlayable && file is ISoundboxPlayable playable)
                {
                    localPlayable.VoiceActivation = FilterUploadVoiceActivationSettings(playable.VoiceActivation);
                }

                //modify in database
                //TODO async
                Database.Update(localFile);

                //update cache and database watermarks (this will call Update for parent)
                SetWatermark(localFile, newWatermark);

                //update our clients
                GetHub().OnFileEvent(new SoundboxFileChangeEvent()
                {
                    Event = SoundboxFileChangeEvent.Type.MODIFIED,
                    File = FlattenForEvent(localFile),
                    PreviousWatermark = previousWatermark
                });

                if (localFile is Sound newSound && localCopy is Sound oldSound)
                {
                    SpeechRecognition_OnSoundChanged(newSound, oldSound);
                }

                return new FileResult(BaseResultStatus.OK, localFile, previousWatermark);
            }
            catch(Exception ex)
            {
                Log(ex);
                return new FileResult(BaseResultStatus.INTERNAL_SERVER_ERROR);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }
        }

        #endregion

        #region "Move"

        /// <summary>
        /// Moves a file to a new directory without performing an <see cref="Edit(SoundboxNode)"/>.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="directory">
        /// Null: uses the root directory instead.
        /// </param>
        /// <returns></returns>
        public async Task<FileResult> Move(SoundboxNode file, SoundboxDirectory directory)
        {
            file = GetCleanFile(file);
            if(file == null)
            {
                return new FileResult(BaseResultStatus.INVALID_PARAMETER);
            }
            if(IsRootDirectory(file))
            {
                return new FileResult(FileResultStatus.ILLEGAL_FILE_EDIT_DENIED_ROOT);
            }

            if(directory == null)
            {
                directory = GetRootDirectory();
            }
            else
            {
                directory = GetCleanFile(directory);
                if(directory == null)
                {
                    return new FileResult(BaseResultStatus.INVALID_PARAMETER);
                }
            }

            if(file == directory)
            {
                //not possible
                return new FileResult(FileResultStatus.MOVE_TARGET_INVALID);
            }
            if(file.ParentDirectory == directory)
            {
                //no change
                return new FileResult(BaseResultStatus.OK_NO_CHANGE);
            }

            try
            {
                DatabaseLock.EnterWriteLock();

                //save previous watermark for event
                Guid previousWatermark = GetRootWatermark();
                Guid newWatermark = Guid.NewGuid();

                //move in cache
                SoundboxDirectory oldParent = file.ParentDirectory;
                oldParent.Children.Remove(file);

                directory.AddChild(file);

                //update file in database
                if(!(file is SoundboxDirectory))
                {
                    //TODO async
                    Database.Update(file);
                }
                //else: is updated anyways in SetWatermak

                //update cache and database watermarks (this will call Update for parent)
                SetWatermark(file, newWatermark);
                SetWatermark(oldParent, newWatermark);

                //update our clients
                GetHub().OnFileEvent(new SoundboxFileMoveEvent()
                {
                    Event = SoundboxFileChangeEvent.Type.MOVED,
                    File = FlattenForEvent(file),
                    FromDirectory = oldParent,
                    PreviousWatermark = previousWatermark
                });

                return new FileResult(BaseResultStatus.OK, file, previousWatermark, oldParent);
            }
            catch(Exception ex)
            {
                Log(ex);
                return new FileResult(BaseResultStatus.INTERNAL_SERVER_ERROR);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }
        }

        #endregion

        #region "Delete"

        /// <summary>
        /// Deletes a file or directory. If <paramref name="file"/> is a directory, then its entire content is deleted as well.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public async Task<FileResult> Delete(SoundboxNode file)
        {
            file = GetCleanFile(file);
            if(file == null)
            {
                return new FileResult(BaseResultStatus.INVALID_PARAMETER);
            }
            if(IsRootDirectory(file))
            {
                return new FileResult(FileResultStatus.ILLEGAL_FILE_EDIT_DENIED_ROOT);
            }

            try
            {
                DatabaseLock.EnterWriteLock();

                //save previous watermark for event
                Guid previousWatermark = GetRootWatermark();
                Guid newWatermark = Guid.NewGuid();

                //remove from cache
                file.ParentDirectory.Children.Remove(file);
                NodesCache.Remove(file.ID);
                //do not remove the parent from the file: need it for the client event

                //remove from disk
                DeleteRecursive(file);

                //delete from database
                //TODO async
                Database.Delete(file);

                //update cache and database watermarks (this will call Update for parent)
                SetWatermark(file, newWatermark);

                //update our clients
                GetHub().OnFileEvent(new SoundboxFileChangeEvent()
                {
                    Event = SoundboxFileChangeEvent.Type.DELETED,
                    File = FlattenForEvent(file),
                    PreviousWatermark = previousWatermark
                });

                return new FileResult(BaseResultStatus.OK, file, previousWatermark);
            }
            catch(Exception ex)
            {
                Log(ex);
                return new FileResult(BaseResultStatus.INTERNAL_SERVER_ERROR);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Deletes the given node from disk.
        /// Deletes the node itself it is a <see cref="SoundboxFile"/>.
        /// Deletes all its content recursively if it is a <see cref="SoundboxDirectory"/>.
        /// </summary>
        /// <param name="node"></param>
        protected void DeleteRecursive(SoundboxNode node)
        {
            if(node is SoundboxFile file)
            {
                try
                {
                    File.Delete(GetAbsoluteFileName(file));
                }
                catch(Exception ex)
                {
                    Log(ex);
                    //continue deleting
                }
            }
            else if(node is SoundboxDirectory directory)
            {
                foreach(var child in directory.Children)
                {
                    DeleteRecursive(child);
                }
            }
        }

        #endregion

        /// <summary>
        /// Fetches the given file (received from a client) from our local database/file system/cache by matching against its <see cref="SoundboxNode.ID"/>.
        /// This is required before pretty much any operation is done on a file received from a client in order to prevent injections of various kinds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        private T GetCleanFile<T>(T file) where T : SoundboxNode
        {
            if (file == null)
                return null;

            try
            {
                DatabaseLock.EnterReadLock();

                if (NodesCache.TryGetValue(file.ID, out var node))
                {
                    return node as T;
                }
            }
            finally
            {
                DatabaseLock.ExitReadLock();
            }

            return null;
        }

        /// <summary>
        /// Returns true if the given node is the root directory. Thus it may not be edited/moved/deleted.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected bool IsRootDirectory(SoundboxNode file)
        {
            if(file is SoundboxDirectory directory)
            {
                return directory.IsRootDirectory();
            }
            return false;
        }

        /// <summary>
        /// <see cref="SoundboxNode.Flatten"/>s the given node but preserves its <see cref="SoundboxNode.ParentDirectory"/> (in a flattened state).
        /// This the form we send out as events when the file tree changes.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        protected SoundboxNode FlattenForEvent(SoundboxNode file)
        {
            return file.Flatten(true);
        }

        #endregion

        #region "Play"

        /// <summary>
        /// Lock used when accessing the playback related data structures (<see cref="PlayersPlaying"/>).
        /// </summary>
        private readonly object PlaybackLock = new object();

        /// <summary>
        /// Keeps track of which sounds are currently being played by whom.
        /// </summary>
        private readonly IDictionary<ISoundChainPlaybackService, PlaybackContext> PlayersPlaying = new IdentityDictionary<ISoundChainPlaybackService, PlaybackContext>();

        /// <summary>
        /// Performs various sanity checks on the given request object and repairs it as well as possible.
        /// Returns null on any error that cannot be recovered from.
        /// Examples:<list type="bullet">
        /// <item>
        /// No item <see cref="SoundPlaybackRequest.Sounds"/> may be null => returns null
        /// </item>
        /// <item>
        /// <see cref="SoundPlayback.Options"/> is null => Set to <see cref="PlaybackOptions.Default"/> instead
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected SoundPlaybackRequest SanityCheck(SoundPlaybackRequest request)
        {
            if (request == null)
                return null;
            if (request.Sounds == null || request.Sounds.Count == 0)
                return null;
            for(int i = 0; i < request.Sounds.Count; ++i)
            {
                var soundPlayback = request.Sounds[i];
                if (soundPlayback == null)
                    return null;

                soundPlayback.Sound = GetCleanFile(soundPlayback.Sound);
                if (soundPlayback.Sound == null)
                    return null;

                if (!soundPlayback.Options.SanityCheck())
                    return null;
            }

            return request;
        }

        /// <summary>
        /// Plays the given request's sound(s)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task Play(Users.User user, SoundPlaybackRequest request)
        {
            request = SanityCheck(request);
            if (request == null)
                return;

            var player = this.ServiceProvider.GetService(typeof(ISoundChainPlaybackService)) as ISoundChainPlaybackService;
            player.PlaybackChanged += (sender, args) =>
            {
                if (args.FromStopGlobal)
                    return;

                //update the sounds the player is currently playing
                //we do that asynchronously to avoid possible locking issues with our PlaybackLock and any lock the playback service maybe using (chance of deadlock otherwise)
                Threading.Tasks.FireAndForget(() =>
                {
                    lock (PlaybackLock)
                    {
                        PlaybackContext playerContext;
                        if(!PlayersPlaying.TryGetValue(player, out playerContext))
                        {
                            //race condition, finished already?
                            return;
                        }

                        if (args.Finished)
                        {
                            PlayersPlaying.Remove(player);
                        }
                        else
                        {
                            playerContext.PlayingNow = args.SoundsPlaying;
                        }

                        FirePlaybackChanged();
                    }
                });
            };

            var context = new PlaybackContext(user, request, player);

            lock(PlaybackLock)
            {
                PlayersPlaying[player] = context;
                player.Play(this, request);
            }
        }

        /// <summary>
        /// Stops the playback of all sounds (<see cref="SoundboxHub.Stop"/>.
        /// </summary>
        /// <returns></returns>
        public async Task Stop()
        {
            lock (PlaybackLock)
            {
                foreach (var player in PlayersPlaying.Keys)
                {
                    player.Stop(true);
                }

                PlayersPlaying.Clear();
                FirePlaybackChanged();
            }
        }

        /// <summary>
        /// Fired whenever the current state of playback changes (new sounds are playing or sounds have stopped).
        /// Fetches the currently plaing sounds from <see cref="PlayersPlaying"/> and updates all clients via <see cref="GetHub"/>.
        /// </summary>
        /// <returns></returns>
        protected Task FirePlaybackChanged()
        {
            return GetHub().OnSoundsPlayingChanged(GetSoundsPlayingNow());
        }

        /// <summary>
        /// Fetches the current playback state from <see cref="PlayersPlaying"/>.
        /// </summary>
        /// <returns></returns>
        public ICollection<PlayingNow> GetSoundsPlayingNow()
        {
            lock (PlaybackLock)
            {
                ICollection<PlayingNow> playingNow = new List<PlayingNow>();

                foreach (var context in PlayersPlaying.Values)
                {
                    playingNow.AddAll(context.GetPlayingNow());
                }

                return playingNow;
            }
        }

        /// <summary>
        /// References a <see cref="Users.User"/> and a <see cref="SoundPlaybackRequest"/> they issued.<br/>
        /// Here we keep track of which sound is currently being played and which <see cref="ISoundChainPlaybackService"/> plays the sounds.
        /// </summary>
        protected class PlaybackContext
        {
            public readonly Users.User User;
            public readonly SoundPlaybackRequest Request;
            public readonly ISoundChainPlaybackService Player;

            public ICollection<SoundPlayback> PlayingNow = new List<SoundPlayback>();

            public PlaybackContext(User user, SoundPlaybackRequest request, ISoundChainPlaybackService player)
            {
                User = user;
                Request = request;
                Player = player;
            }

            /// <summary>
            /// Transforms the current status into a collection of <see cref="PlayingNow"/> that can be sent to clients as an event.
            /// </summary>
            /// <returns></returns>
            public ICollection<PlayingNow> GetPlayingNow()
            {
                ICollection<PlayingNow> playingNow = new List<PlayingNow>();

                foreach(var sound in this.PlayingNow)
                {
                    playingNow.Add(new PlayingNow()
                    {
                        Sound = sound.Sound,
                        User = new PlayingNow.FromUser(this.User)
                    });
                }

                return playingNow;
            }
        }

        #endregion

        #region "Volume"

        /// <summary>
        /// Maximum volume modifier. Is multiplied with the volume given in <see cref="SetVolume(int)"/>.
        /// </summary>
        protected int VolumeSettingMax = int.MinValue;

        protected const string PREFERENCES_KEY_VOLUME_MAX = "Soundbox.Volume.Max";

        /// <summary>
        /// Sets the current global system volume thus adjusting the playback volume of all currently active sounds.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public async Task<ServerResult> SetVolume(int volume)
        {
            volume = (int)Volume.Limit(volume);

            double volumeMax = await GetVolumeSettingMax();
            double volumeModified = Volume.GetVolume(volume, volumeMax);

            var volumeService = ServiceProvider.GetService(typeof(IVolumeService)) as IVolumeService;
            await volumeService.SetVolume(volumeModified);

            //update our clients
            GetHub().OnVolumeChanged(volume);

            return new ServerResult(BaseResultStatus.OK);
        }

        /// <summary>
        /// Returns the current system volume (i.e. the volume passed last to <see cref="SetVolume(int)"/>).
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetVolume()
        {
            var volumeService = ServiceProvider.GetService(typeof(IVolumeService)) as IVolumeService;
            double volume = await volumeService.GetVolume();

            double volumeMax = await GetVolumeSettingMax();
            return (int) Math.Round(Volume.GetVolumeOriginal(volume, volumeMax));
        }

        /// <summary>
        /// Returns <see cref="VolumeSettingMax"/> and loads it from the preferences (<see cref="IPreferencesProvider{T}"/>) first if required.<br/>
        /// This max volume multiplied with <see cref="GetVolume"/> results in the actual current system volume.
        /// </summary>
        /// <returns></returns>
        public async Task<int> GetVolumeSettingMax()
        {
            if(VolumeSettingMax != int.MinValue)
            {
                return VolumeSettingMax;
            }

            var preferences = ServiceProvider.GetService(typeof(IPreferencesProvider<int>)) as IPreferencesProvider<int>;
            if(await preferences.Contains(PREFERENCES_KEY_VOLUME_MAX))
            {
                int setting = await preferences.Get(PREFERENCES_KEY_VOLUME_MAX);
                setting = (int)Volume.Limit(setting);

                VolumeSettingMax = setting;
            }
            else
            {
                VolumeSettingMax = Constants.VOLUME_MAX;
            }

            return VolumeSettingMax;
        }

        /// <summary>
        /// Adjusts <see cref="VolumeSettingMax"/>. Playback volume of all active sounds is adjusted immediately.
        /// </summary>
        /// <seealso cref="GetVolumeSettingMax"/>
        public async Task<ServerResult> SetVolumeSettingMax(int volumeSettingMax)
        {
            volumeSettingMax = (int) Volume.Limit(volumeSettingMax);

            //get the last value of SetVolume. need that to adjust the current playback volume accordingly
            int currentVolumeSetting = await GetVolume();

            var preferences = ServiceProvider.GetService(typeof(IPreferencesProvider<int>)) as IPreferencesProvider<int>;
            await preferences.Set(PREFERENCES_KEY_VOLUME_MAX, volumeSettingMax);
            this.VolumeSettingMax = volumeSettingMax;

            //update our clients
            GetHub().OnSettingMaxVolumeChanged(volumeSettingMax);
            //update the current playback volume
            await SetVolume(currentVolumeSetting);

            return new ServerResult(BaseResultStatus.OK);
        }

        #endregion

        #region "Speech Recognition"

        protected ISpeechRecognitionServiceProvider speechProvider;
        protected ISpeechRecognitionService speechRecognizer;

        /// <summary>
        /// Called on application start to create an instance of <see cref="ISpeechRecognitionService"/> if enabled in the current configuration.
        /// Then starts the speech recognition via <see cref="ISpeechRecognitionService.Start(SpeechRecognitionOptions)"/>
        /// </summary>
        /// <param name="provider"></param>
        protected void SpeechRecognition_Setup(ISpeechRecognitionServiceProvider provider)
        {
            if (provider == null)
                //nothing to do, not installed
                return;

            if (AppSettings.SpeechRecognition?.AudioDevice == null || AppSettings?.SpeechRecognition?.Languages == null || AppSettings.SpeechRecognition.Languages.Count == 0)
            {
                Logger.LogInformation("No Soundbox.SpeechRecognition config");
                return;
            }

            var config = new SpeechRecognitionConfig()
            {
                AudioSource = AppSettings.SpeechRecognition.AudioDevice,
                VolumeThreshold = AppSettings.SpeechRecognition.VolumeThreshold
            };

            var recognizer = provider.GetSpeechRecognizer(config);
            var matcher = ServiceProvider.GetService(typeof(SpeechRecognitionMatcher)) as SpeechRecognitionMatcher;
            if (recognizer == null || matcher == null)
            {
                //no speech recognizer is installed for the current config
                recognizer?.Dispose();
                return;
            }

            recognizer.Recognized += (sender, e) =>
            {
                //try and match the spoken words
                var match = matcher.Match(e,
                    //detect sounds...
                    NodesCache.Values.WhereIs<ISpeechRecognizable>()
                    //...and special commands
                    .Concat(SpeechRecognition_GetSpecialCommands()));
                if (match.Success)
                {
                    _ = SpeechRecognition_OnRecognized(match.Recognizable);
                }
            };

            var options = SpeechRecognition_GetOptions();

            speechProvider = provider;
            speechRecognizer = recognizer;
            recognizer.Start(options);
        }

        /// <summary>
        /// Returns speech recognition options for the current config and set of sounds (phrases).
        /// </summary>
        /// <returns></returns>
        protected SpeechRecognitionOptions SpeechRecognition_GetOptions()
        {
            return new SpeechRecognitionOptions()
            {
                Languages = AppSettings.SpeechRecognition.Languages,
                Phrases = NodesCache.Values.WhereIs<ISoundboxPlayable>()
                    .Where(sound => sound.VoiceActivation != null)
                    .Select(sound => sound.VoiceActivation)
                    //add special commands
                    .Concat(SpeechRecognition_GetSpecialCommands())
                    //filter null/empty lists
                    .Where(command => command.SpeechPhrases?.Count > 0)
                    .SelectMany(command => command.SpeechPhrases)
                    .ToList()
            };
        }

        /// <summary>
        /// Called when a sound has been added or changed.
        /// Updates our speech recognition with the modified phrases if required.
        /// </summary>
        /// <param name="soundNew"></param>
        /// <param name="soundOld"></param>
        protected void SpeechRecognition_OnSoundChanged(Sound soundNew, Sound soundOld)
        {
            bool phrasesChanged = false;

            bool newHasPhrases = soundNew.VoiceActivation?.SpeechPhrases.Count > 0;
            bool oldHasPhrases = soundOld?.VoiceActivation?.SpeechPhrases.Count > 0;

            phrasesChanged = newHasPhrases != oldHasPhrases;

            if (phrasesChanged && speechRecognizer != null)
            {
                speechRecognizer.UpdateOptions(SpeechRecognition_GetOptions());
            }
        }

        #region "Handle events"

        /// <summary>
        /// Called when the given voice-activated command has been recognized.
        /// Executes the command (e.g., plays a sound).
        /// </summary>
        /// <param name="recognized"></param>
        /// <returns></returns>
        protected async Task SpeechRecognition_OnRecognized(ISpeechRecognizable recognized)
        {
            if (recognized is Sound sound)
            {
                await Play(new User(), new SoundPlaybackRequest()
                {
                    Sounds = new List<SoundPlayback>()
                    {
                        new SoundPlayback()
                        {
                            Sound = sound
                        }
                    }
                });
            }
            else if (recognized is SpeechRecognitionStopCommand)
            {
                await Stop();
            }
        }

        #endregion

        #region "Special commands"

        /// <summary>
        /// Returns a list of all "special" commands (such as <see cref="SpeechRecognition_GetStopCommand"/>)
        /// that should be considered during the voice recognition.
        /// </summary>
        /// <returns></returns>
        protected ICollection<SoundboxVoiceActivation> SpeechRecognition_GetSpecialCommands()
        {
            var commands = new List<SoundboxVoiceActivation>();

            var stopCommand = SpeechRecognition_GetStopCommand();
            if (stopCommand?.SpeechTriggers?.Count > 0)
                commands.Add(stopCommand);

            return commands;
        }

        #region "Stop"

        /// <summary>
        /// Returns the configured "stop" command (if any) that stops all playback.
        /// </summary>
        /// <returns></returns>
        protected SoundboxVoiceActivation SpeechRecognition_GetStopCommand()
        {
            var stopConfig = AppSettings.SpeechRecognition.StopVoiceCommand;
            if (stopConfig != null)
                return new SpeechRecognitionStopCommand(stopConfig);
            return null;
        }

        /// <summary>
        /// Doesn't do anything, detected by <see cref="SpeechRecognition_OnRecognized(ISpeechRecognizable)"/>.
        /// </summary>
        protected class SpeechRecognitionStopCommand : SoundboxVoiceActivation
        {
            public SpeechRecognitionStopCommand(SoundboxVoiceActivation config) : base(config)
            {
            }
        }

        #endregion

        #endregion

        #region "Client Test"

        /// <summary>
        /// Uploads an audio file. The audio file will be processed by the soundbox's speech detection (if installed)
        /// and the recognized words and matched <paramref name="recognizables"/> will be streamed back.
        /// </summary>
        /// <param name="audio">
        /// Should usually be a <see cref="Audio.AudioBlob"/> from an uploaded file.
        /// </param>
        /// <param name="recognizables"></param>
        /// <param name="phrases">
        /// See <see cref="SoundboxVoiceActivation.SpeechPhrases"/>
        /// </param>
        /// <returns></returns>
        public IAsyncEnumerable<SpeechRecognitionTestResult> SpeechRecognition_ClientTest(Audio.IAudioSource audio, ICollection<SpeechRecognitionTestRecognizable> recognizables, ICollection<string> phrases,
            CancellationToken cancellationToken = default)
        {
            var buffer = Channel.CreateUnbounded<SpeechRecognitionTestResult>();

            RunSpeechRecognition();

            return buffer.Reader.ReadAllAsync(cancellationToken);

            async void RunSpeechRecognition()
            {
                var matcher = ServiceProvider.GetService(typeof(SpeechRecognitionMatcher)) as SpeechRecognitionMatcher;
                if (speechProvider == null || matcher == null)
                {
                    //not installed
                    await buffer.Writer.WriteAsync(new SpeechRecognitionTestResult(SpeechRecognitionTestResultStatus.NOT_AVAILABLE));
                    buffer.Writer.Complete();
                    return;
                }

                var config = new SpeechRecognitionConfig()
                {
                    AudioSource = audio
                };

                var speechRecognizer = speechProvider.GetSpeechRecognizer(config);
                if (speechRecognizer == null)
                {
                    //audio is not supported
                    await buffer.Writer.WriteAsync(new SpeechRecognitionTestResult(SpeechRecognitionTestResultStatus.INVALID_AUDIO_TYPE));
                    buffer.Writer.Complete();
                    return;
                }

                //register on speech events and feed the output
                speechRecognizer.Recognized += async (s, e) =>
                {
                    //perform a matching and return the result
                    var match = matcher.Match(e, recognizables);
                    await buffer.Writer.WriteAsync(new SpeechRecognitionTestResult(BaseResultStatus.OK, e, match, false));
                };

                speechRecognizer.Stopped += async (s, e) =>
                {
                    ResultStatus status = BaseResultStatus.OK;
                    if (e.Exception != null)
                    {
                        status = BaseResultStatus.INTERNAL_SERVER_ERROR;
                    };

                    await buffer.Writer.WriteAsync(new SpeechRecognitionTestResult(status, null, null, true));

                    //all done: clean up
                    buffer.Writer.Complete();
                    speechRecognizer.Dispose();
                };

                //start the recognition
                var options = new SpeechRecognitionOptions()
                {
                    //use the same languages here as we do for the background recognition.
                    //if we were using a user-provided language here, the user might get misleading results
                    Languages = AppSettings.SpeechRecognition.Languages,
                    Phrases = FilterUploadVoiceActivationStrings(phrases)
                };

                await speechRecognizer.Start(options);

                //stop recognizing when requested
                cancellationToken.Register(async () =>
                {
                    await speechRecognizer.Stop();
                });
            };
        }

        #endregion

        #endregion

        protected void Log(Exception ex)
        {
            Logger.LogError(ex, "Soundbox error");
        }
    }
}
