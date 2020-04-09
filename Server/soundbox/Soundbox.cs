using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Soundbox.Users;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Singleton class that manages a list/tree of available sounds and handles playback.
    /// Clients are notified of the playback state etc via <see cref="SoundboxHub"/>
    /// </summary>
    public class Soundbox : SoundboxContext, ISoundboxHubProvider
    {
        protected static readonly ICollection<string> SUPPORTED_FILE_TYPES = new string[]
        {
            "mp3",
            "wav",
            "ogg",
            "aac"
        };

        protected IHubContext<SoundboxHub, ISoundboxClient> HubContext;

        protected IServiceProvider ServiceProvider;

        /// <summary>
        /// Absolute path of the soundbox's root directory.
        /// This is not however the root directory of <see cref="SoundboxFile"/>s
        /// (see <see cref="SoundsRootDirectory"/>).
        /// </summary>
        protected string BaseDirectory;

        /// <summary>
        /// The root directory for the entire <see cref="SoundboxFile"/> tree.
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
            IDatabaseProvider database)
        {
            this.ServiceProvider = serviceProvider;
            this.HubContext = hubContext;

            this.BaseDirectory = config.GetRootDirectory();
            this.SoundsRootDirectory = this.BaseDirectory + "sounds/";
            Directory.CreateDirectory(this.SoundsRootDirectory);

            this.Database = database;
        }

        public ISoundboxClient GetHub()
        {
            return HubContext.Clients.All;
        }

        #region "Sounds Database"

        /// <summary>
        /// Lock used when accessing the file structure or our (in-process) database or our in-process cache (<see cref="GetSoundsTree"/>).
        /// </summary>
        protected ReaderWriterLockSlim DatabaseLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        protected SoundboxDirectory SoundsRoot;

        /// <summary>
        /// Returns the root directory of the <see cref="SoundboxFile"/> tree.
        /// </summary>
        /// <returns></returns>
        public SoundboxDirectory GetSoundsTree()
        {
            try
            {
                DatabaseLock.EnterReadLock();

                if (SoundsRoot == null)
                {
                    //SoundsRoot = ReadTreeFromDisk();
                    //TODO async
                    var task = Database.Get();
                    SoundsRoot = task.Result;

                    if(SoundsRoot == null)
                    {
                        //database is empty => create new root directory
                        SoundsRoot = new SoundboxDirectory();
                        SoundsRoot.ID = Guid.NewGuid();
                        SoundsRoot.Watermark = Guid.NewGuid();

                        //TODO async
                        Database.Insert(SoundsRoot);
                    }
                }
                return SoundsRoot;
            }
            finally
            {
                DatabaseLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Alias for <see cref="GetSoundsTree"/>.
        /// </summary>
        /// <returns></returns>
        protected SoundboxDirectory GetRootDirectory()
        {
            return GetSoundsTree();
        }

        /// <summary>
        /// Sets the given watermark on all ancestors of a file (and the file itself if it represents a directory). This is required when the given file changed.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="watermark"></param>
        protected void SetWatermark(SoundboxFile file, Guid watermark)
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
        /// Returns the root directory of the <see cref="SoundboxFile"/> tree by reading it from disk.
        /// "From disk" in this context can mean reading the actual file tree or querying a local database if any.
        /// In any case all files in the returned tree have been verified to exist.
        /// </summary>
        /// <returns></returns>
        public SoundboxDirectory ReadTreeFromDisk()
        {
            try
            {
                DatabaseLock.EnterReadLock();

                SoundboxDirectory rootDirectory = new SoundboxDirectory();
                rootDirectory.ID = Guid.NewGuid();
                rootDirectory.AddChildren(ReadDirectoryFromDisk(SoundsRootDirectory));

                //read from disk => set new watermark for entire tree
                SetWatermarkDescendants(rootDirectory, Guid.NewGuid());

                return rootDirectory;
            }
            finally
            {
                DatabaseLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Recursively sets the given history watermark in the given directory and all its <b>descendants</b>.<br/>
        /// Usually this should only be used when fetching an entirely new directory from disk as opposed from our database.
        /// In other case any change in a file/directory would recursively update the <b>ancestors</b> instead.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="watermark"></param>
        private void SetWatermarkDescendants(SoundboxDirectory directory, Guid watermark)
        {
            directory.Watermark = watermark;

            foreach (var child in directory.Children)
            {
                var childDirectory = child as SoundboxDirectory;

                if (childDirectory != null)
                {
                    SetWatermarkDescendants(childDirectory, watermark);
                }
            }
        }

        /// <summary>
        /// Recursively reads the given directory's content from the hard disk.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private ICollection<SoundboxFile> ReadDirectoryFromDisk(string directory)
        {
            ICollection<SoundboxFile> soundboxFiles = new List<SoundboxFile>();

            List<string> files = new List<string>();
            try
            {
                files.AddRange(Directory.GetFiles(directory));
                files.AddRange(Directory.GetDirectories(directory));
            }
            catch(Exception e)
            {
                //directory does not exist => treat as empty
                Log(e);
                return soundboxFiles;
            }

            foreach(var file in files)
            {
                SoundboxFile soundboxFile;

                if(Directory.Exists(file))
                {
                    //is directory
                    SoundboxDirectory soundboxDirectory = new SoundboxDirectory();

                    try
                    {
                        soundboxDirectory.AddChildren(ReadDirectoryFromDisk(file));
                    }
                    catch(Exception e)
                    {
                        Log(e);
                    }

                    soundboxFile = soundboxDirectory;
                }
                else if(IsSoundFile(file))
                {
                    //is sound
                    Sound sound = new Sound();

                    soundboxFile = sound;
                }
                else
                {
                    continue;
                }

                //set random ID when reading new file from disk
                soundboxFile.ID = Guid.NewGuid();
                soundboxFile.FileName = MakeRelativePath(file, directory);
                soundboxFile.AbsoluteFileName = MakeRelativePath(file, SoundsRootDirectory);

                //TODO: format name
                soundboxFile.Name = soundboxFile.FileName;

                soundboxFiles.Add(soundboxFile);
            }

            return soundboxFiles;
        }

        /// <summary>
        /// Checks whether the given file is a sound file of a supported type.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool IsSoundFile(string file)
        {
            //TODO
            return File.Exists(file);
        }

        /// <summary>
        /// Returns the file's path relative to the given directory
        /// (returns the file's name).
        /// </summary>
        /// <param name="file"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        protected string MakeRelativePath(string file, string directory)
        {
            string path = Path.GetRelativePath(directory, file);
            if (path.StartsWith(@"./"))
                path = Regex.Replace(path, @"^\./", "");
            else if (path.StartsWith(@".\"))
                path = Regex.Replace(path, @"^\.\\", "");
            else if (path.StartsWith(@"\"))
                path = Regex.Replace(path, @"^\\", "");

            return path;
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
        /// <item><see cref="SoundboxFile.Name"/></item>
        /// <item><see cref="SoundboxFile.FileName"/></item>
        /// <item><see cref="SoundboxFile.Tags"/></item>
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

            sound.ID = Guid.NewGuid();
            sound.ParentDirectory = directory;
            sound.FileName = NormalizeFileName(sound.FileName);
            sound.FileName = MakeFileNameUnique(sound);
            sound.UpdateAbsoluteFileName();

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
        private FileResult UploadOnNewFile(SoundboxFile newFile, SoundboxDirectory parent)
        {
            try
            {
                DatabaseLock.EnterWriteLock();

                //save previous watermark for event
                Guid previousWatermark = GetSoundsTree().Watermark;
                Guid newWatermark = Guid.NewGuid();

                //add to cache
                parent.AddChild(newFile);
                //add to database
                //TODO async
                Database.Insert(newFile);

                //update cache and database watermarks
                SetWatermark(newFile, newWatermark);

                //update our clients
                GetHub().OnFileEvent(new SoundboxFileChangeEvent()
                {
                    Event = SoundboxFileChangeEvent.Type.ADDED,
                    File = newFile,
                    PreviousWatermark = previousWatermark
                });

                return new FileResult(BaseResultStatus.OK, newFile, previousWatermark);
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Creates a unique file name for the given file upon upload. It is guaranteed to be globally unique.
        /// </summary>
        /// <param name="sound"></param>
        /// <returns></returns>
        protected string MakeFileNameUnique(SoundboxFile file)
        {
            //"my_sound.mp3" -> "my_sound_MYUID.mp3"
            string name = GetFileNamePure(file.FileName);
            return Regex.Replace(file.FileName, "^" + Regex.Escape(name), name + "_" + file.ID.ToString());
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
        /// Checks if the given <see cref="SoundboxFile.Name"/> is valid (e.g. is not empty...)
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
        /// <item><see cref="SoundboxFile.Name"/></item>
        /// <item><see cref="SoundboxFile.Tags"/></item>
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
            if(!CheckUploadDirectoryName(directory.Name))
            {
                //error
                return new FileResult(FileResultStatus.INVALID_FILE_NAME);
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
            directory.Children = new List<SoundboxFile>();
            directory.ParentDirectory = parent;
            directory.FileName = NormalizeFileName(directory.Name);
            directory.FileName = MakeFileNameUnique(directory);
            directory.UpdateAbsoluteFileName();

            ResultStatus status = BaseResultStatus.INTERNAL_SERVER_ERROR;

            try
            {
                DatabaseLock.EnterWriteLock();

                //create the directory on disk
                try
                {
                    Directory.CreateDirectory(GetAbsoluteFileName(directory));
                }
                catch
                {
                    status = FileResultStatus.IO_ERROR;
                    throw;
                }

                return UploadOnNewFile(directory, parent);
            }
            catch (Exception ex)
            {
                Log(ex);
                //delete on error
                try
                {
                    Directory.Delete(GetAbsoluteFileName(directory), false);
                }
                catch { }
            }
            finally
            {
                DatabaseLock.ExitWriteLock();
            }

            return new FileResult(status);
        }

        /// <summary>
        /// Checks if the given file name of a new directory is valid (e.g. is not empty...)
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        protected bool CheckUploadDirectoryName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return true;
        }

        #endregion

        /// <summary>
        /// Fetches the given file (received from a client) from our local database/file system/cache by matching against its <see cref="SoundboxFile.ID"/>.
        /// This is required before pretty much any operation is done on a file received from a client in order to prevent injections of various kinds.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        private T GetCleanFile<T>(T file) where T : SoundboxFile
        {
            if (file == null)
                return null;

            return GetCleanFileFromDirectory(GetSoundsTree(), file) as T;
        }

        /// <summary>
        /// Recursive search function for <see cref="GetCleanFile{T}(T)"/>.
        /// For testing only until we implemented a look up table.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        private SoundboxFile GetCleanFileFromDirectory(SoundboxDirectory directory, SoundboxFile file)
        {
            if (directory.ID == file.ID)
                return directory;

            foreach(var child in directory.Children)
            {
                if (child.ID == file.ID)
                    return child;
                if(child is SoundboxDirectory)
                {
                    var result = GetCleanFileFromDirectory(child as SoundboxDirectory, file);
                    if (result != null)
                        return result;
                }
            }

            return null;
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
        protected int VolumeSettingMax = -1;

        protected const string PREFERENCES_KEY_VOLUME_MAX = "Soundbox.Volume.Max";

        /// <summary>
        /// Sets the current global system volume thus adjusting the playback volume of all currently active sounds.
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public async Task SetVolume(int volume)
        {
            //TODO check value, error
            double volumeMax = await GetVolumeSettingMax();
            double volumeModified = Volume.GetVolume(volume, volumeMax);

            var volumeService = ServiceProvider.GetService(typeof(IVolumeService)) as IVolumeService;
            await volumeService.SetVolume(volumeModified);

            //update our clients
            GetHub().OnVolumeChanged(volume);
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
            if(VolumeSettingMax >= 0)
            {
                return VolumeSettingMax;
            }

            var preferences = ServiceProvider.GetService(typeof(IPreferencesProvider<int>)) as IPreferencesProvider<int>;
            if(await preferences.Contains(PREFERENCES_KEY_VOLUME_MAX))
            {
                int setting = await preferences.Get(PREFERENCES_KEY_VOLUME_MAX);
                if (setting > 100)
                    setting = 100;
                else if (setting < 0)
                    setting = 0;

                VolumeSettingMax = setting;
            }
            else
            {
                VolumeSettingMax = 100;
            }

            return VolumeSettingMax;
        }

        /// <summary>
        /// Adjusts <see cref="VolumeSettingMax"/>. Playback volume of all active sounds is adjusted immediately.
        /// </summary>
        /// <seealso cref="GetVolumeSettingMax"/>
        public async Task SetVolumeSettingMax(int volumeSettingMax)
        {
            //TODO check value, error
            //get the last value of SetVolume. need that to adjust the current playback volume accordingly
            int currentVolumeSetting = await GetVolume();

            var preferences = ServiceProvider.GetService(typeof(IPreferencesProvider<int>)) as IPreferencesProvider<int>;
            await preferences.Set(PREFERENCES_KEY_VOLUME_MAX, volumeSettingMax);

            //update our clients
            GetHub().OnSettingMaxVolumeChanged(volumeSettingMax);
            //update the current playback volume
            await SetVolume(currentVolumeSetting);
        }

        #endregion

        protected void Log(Exception ex)
        {
            //TODO
        }
    }

    public interface ISoundboxHubProvider
    {
        ISoundboxClient GetHub();
    }
}
