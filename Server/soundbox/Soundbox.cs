using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Singleton class that manages a list/tree of available sounds and handles playback.
    /// Clients are notified of the playback state etc via <see cref="SoundboxHub"/>
    /// </summary>
    public class Soundbox : SoundboxContext, ISoundboxHubProvider
    {
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

        public Soundbox(
            IServiceProvider serviceProvider,
            IHubContext<SoundboxHub, ISoundboxClient> hubContext,
            IConfiguration config)
        {
            this.ServiceProvider = serviceProvider;
            this.HubContext = hubContext;

            //TODO paths from config
            this.BaseDirectory = "~/.soundbox/";
            this.SoundsRootDirectory = this.BaseDirectory + "sounds/";
        }

        public ISoundboxClient GetHub()
        {
            return HubContext.Clients.All;
        }

        public override string GetSoundsRootDirectory()
        {
            return SoundsRootDirectory;
        }

        /// <summary>
        /// Returns the root directory of the <see cref="SoundboxFile"/> tree.
        /// </summary>
        /// <returns></returns>
        public SoundboxDirectory GetSoundsTree()
        {
            return ReadTreeFromDisk();
        }

        /// <summary>
        /// Returns the root directory of the <see cref="SoundboxFile"/> tree by reading it from disk.
        /// "From disk" in this context can mean reading the actual file tree or querying a local database if any.
        /// In any case all files in the returned tree have been verified to exist.
        /// </summary>
        /// <returns></returns>
        public SoundboxDirectory ReadTreeFromDisk()
        {
            SoundboxDirectory rootDirectory = new SoundboxDirectory();
            rootDirectory.AddChildren(ReadDirectoryFromDisk(SoundsRootDirectory));

            //read from disk => set new watermark for entire tree
            SetWatermark(rootDirectory, Guid.NewGuid());

            return rootDirectory;
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
        /// Recursively sets the given history watermark in the given directory and all its descendants.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="watermark"></param>
        private void SetWatermark(SoundboxDirectory directory, Guid watermark)
        {
            directory.Watermark = watermark;

            foreach(var child in directory.Children)
            {
                var childDirectory = child as SoundboxDirectory;

                if (childDirectory != null)
                {
                    SetWatermark(childDirectory, watermark);
                }
            }
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
        /// Plays the given request's sound(s)
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task Play(SoundPlaybackRequest request)
        {
            //foreach(var sound in request.Sounds)
            //{
            //    string fileName = SoundsRootDirectory + sound.Sound.AbsoluteFileName;

            //    System.Media.SoundPlayer player = new System.Media.SoundPlayer(fileName);
            //    player.Play();
            //}
            var player = this.ServiceProvider.GetService(typeof(ISoundChainPlaybackService)) as ISoundChainPlaybackService;
            player.Play(this, request);
        }

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
