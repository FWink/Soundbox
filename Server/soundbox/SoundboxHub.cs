﻿using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    public class SoundboxHub : Hub<ISoundboxClient>
    {
        protected Soundbox GetSoundbox()
        {
            return (Soundbox) Context.GetHttpContext().RequestServices.GetService(typeof(Soundbox));
        }

        /// <summary>
        /// Retrieves the current context's soundbox user.
        /// </summary>
        /// <returns></returns>
        protected Users.User GetUser()
        {
            //TODO
            return new Users.User();
        }

        /// <summary>
        /// Returns the given directory's children (i.e. fetches <see cref="SoundboxDirectory.Children"/>).
        /// </summary>
        /// <param name="directory">The directory content to fetch. Null for base directory</param>
        /// <param name="recursive">Whether to recursively fetch the entire file branch.</param>
        /// <returns>
        /// The directory's content. If null is passed for the <paramref name="directory"/> (requesting the root directory) and <paramref name="recursive"/> is false
        /// then only the root directory is returned without its children. That can be utilized to quickly verify whether the local sound list is up-to-date by checking the root directory's <see cref="SoundboxDirectory.Watermark"/>.
        /// </returns>
        public async Task<ICollection<SoundboxNode>> GetSounds(SoundboxDirectory directory = null, bool recursive = false)
        {
            SoundboxDirectory serverDirectory = await GetSoundbox().GetDirectory(directory);

            if(directory == null && recursive)
            {
                //return the entire tree
                return new SoundboxNode[]
                {
                    serverDirectory
                };
            }

            if(directory == null)
            {
                //root directory without children
                return new SoundboxNode[]
                {
                    serverDirectory.Flatten()
                };
            }

            //return the requested directory's children
            if (serverDirectory == null)
                //invalid directory has been passed
                return null;

            if (recursive)
                return serverDirectory.Children;
            //else: flatten the result; do not return the children's children

            ICollection<SoundboxNode> children = new List<SoundboxNode>();
            foreach(var child in serverDirectory.Children)
            {
                children.Add(FlattenNode(child));
            }

            return children;
        }

        /// <summary>
        /// Flattens the given <see cref="SoundboxNode"/>: if it is a <see cref="SoundboxFile"/> it is returned without modification.
        /// If it is a <see cref="SoundboxDirectory"/> then a copy without children is returned.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected SoundboxNode FlattenNode(SoundboxNode node)
        {
            if(node is SoundboxDirectory directory)
            {
                return directory.Flatten();
            }
            else
            {
                return node;
            }
        }

        /// <summary>
        /// Returns all sounds that are currently being played.
        /// </summary>
        /// <returns></returns>
        public ICollection<PlayingNow> GetSoundsPlayingNow()
        {
            return GetSoundbox().GetSoundsPlayingNow();
        }

        /// <summary>
        /// Plays a single or multiple sounds.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public Task Play(SoundPlaybackRequest request)
        {
            return GetSoundbox().Play(GetUser(), request);
        }

        /// <summary>
        /// Gets the current volume level (<see cref="SetVolume(int)"/>).
        /// </summary>
        /// <returns></returns>
        public Task<int> GetVolume()
        {
            return GetSoundbox().GetVolume();
        }

        /// <summary>
        /// Sets the current volume on a scale from 0-100
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public Task SetVolume(int volume)
        {
            return GetSoundbox().SetVolume(volume);
        }

        /// <summary>
        /// Gets the current maximum system volume (<see cref="SetSettingMaxVolume(int)"/>).
        /// </summary>
        /// <returns></returns>
        public Task<int> GetSettingMaxVolume()
        {
            return GetSoundbox().GetVolumeSettingMax();
        }

        /// <summary>
        /// Sets the maximum system volume (0-100). This affects the effect of <see cref="SetVolume(int)"/>:
        /// the value given in <see cref="SetVolume(int)"/> is mapped to a scale from 0-MAXVOLUME instead
        /// (e.g. if both are 50 then the actual volume is 25).
        /// </summary>
        /// <param name="volume"></param>
        /// <returns></returns>
        public Task SetSettingMaxVolume(int volume)
        {
            return GetSoundbox().SetVolumeSettingMax(volume);
        }

        /// <summary>
        /// Immediately stops all playback.
        /// </summary>
        /// <returns></returns>
        public Task Stop()
        {
            return GetSoundbox().Stop();
        }

        /// <summary>
        /// Deletes the given sound or directory. When a directory is passed then all content is deleted recursively.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Task<FileResult> Delete(SoundboxNode file)
        {
            return GetSoundbox().Delete(file);
        }

        /// <summary>
        /// Edits the given file. Currently these attributes are affected:<list type="bullet">
        /// <item><see cref="SoundboxNode.Name"/></item>
        /// <item><see cref="SoundboxNode.Tags"/></item>
        /// </list>
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public Task<FileResult> Edit(SoundboxNode file)
        {
            return GetSoundbox().Edit(file);
        }

        /// <summary>
        /// Moves a file to a new directory. There is no <see cref="Edit(SoundboxNode)"/> performed on the given file.<br/>
        /// If the given directory is null then the root directory is used.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="directory"></param>
        /// <returns></returns>
        public Task<FileResult> Move(SoundboxNode file, SoundboxDirectory directory)
        {
            return GetSoundbox().Move(file, directory);
        }

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
            return GetSoundbox().MakeDirectory(directory, parent);
        }
    }
}
