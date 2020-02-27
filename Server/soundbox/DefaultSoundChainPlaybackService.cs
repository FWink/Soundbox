using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Soundbox
{
    public class DefaultSoundChainPlaybackService : ISoundChainPlaybackService
    {
        protected IServiceProvider ServiceProvider;

        /// <summary>
        /// True: the entire chain has finished playing either naturally or via <see cref="Stop(bool)"/>.
        /// </summary>
        protected bool Finished = false;

        /// <summary>
        /// Count of sounds that have finished their playback naturally.
        /// </summary>
        protected int SoundsFinished = 0;

        /// <summary>
        /// Sounds that are playing right now.
        /// </summary>
        protected ICollection<SoundPlayback> SoundsPlaying = new List<SoundPlayback>();

        /// <summary>
        /// The <see cref="ISoundPlaybackService"/>s currently playing <see cref="SoundsPlaying"/>.
        /// </summary>
        protected ISet<ISoundPlaybackService> PlayersPlaying = new IdentityDictionary<ISoundPlaybackService, bool>().ToSet();

        public DefaultSoundChainPlaybackService(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Returns a <see cref="ISoundPlaybackService"/> that can be used to play a single sound.
        /// </summary>
        /// <returns></returns>
        protected ISoundPlaybackService GetSoundService()
        {
            return ServiceProvider.GetService(typeof(ISoundPlaybackService)) as ISoundPlaybackService;
        }

        public event EventHandler<ISoundChainPlaybackService.PlaybackEventArgs> PlaybackChanged;

        public void Play(SoundboxContext context, SoundPlaybackRequest sounds)
        {
            if(sounds.Sounds.Count == 0)
            {
                PlaybackChanged?.Invoke(this, new ISoundChainPlaybackService.PlaybackEventArgs(
                    soundsPlaying: new List<SoundPlayback>(),
                    finished: true,
                    fromStop: false,
                    fromStopGlobal: false
                ));
                return;
            }

            Play(context, sounds, 0);
        }

        /// <summary>
        /// Plays the sound at the given index and recursively calls <see cref="Play(SoundboxContext, SoundPlaybackRequest, int)"/> again
        /// to play the next sound as well as required.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sounds"></param>
        /// <param name="index"></param>
        protected void Play(SoundboxContext context, SoundPlaybackRequest sounds, int index)
        {
            if(Finished)
            {
                //aborted
                return;
            }

            var sound = sounds.Sounds[index];
            //whether to play the next sound when we're done here (i.e. this is not the last track of the chain)
            bool continueNextSound = index < (sounds.Sounds.Count - 1);
            //whether to use SoundService's callback to start the next sound (more accurate) or to use start a timer on our own to play the next sound in the chain
            bool continueInCallback = continueNextSound && sound.Options.ChainDelayMs == 0;

            var player = GetSoundService();
            player.PlaybackFinished += (sender, args) =>
            {
                if(args.FromStop)
                {
                    //nothing to do. handled in Stop() already
                    return;
                }

                bool finished = false;
                ICollection<SoundPlayback> soundsPlaying;

                lock (this)
                {
                    SoundsPlaying.Remove(sound);
                    PlayersPlaying.Remove(player);

                    if(++SoundsFinished == sounds.Sounds.Count)
                    {
                        //this was the last sound. we're all done now
                        finished = true;
                    }

                    if(!this.Finished)
                    {
                        this.Finished = finished;

                        if(!continueInCallback)
                        {
                            //don't fire the STOPPED event if we would send the START event right away anyway
                            soundsPlaying = new List<SoundPlayback>(this.SoundsPlaying);

                            //update our listeners
                            PlaybackChanged?.Invoke(this, new ISoundChainPlaybackService.PlaybackEventArgs(
                                soundsPlaying: soundsPlaying,
                                finished: finished,
                                fromStop: false,
                                fromStopGlobal: false
                            ));
                        }
                    }
                }

                if (continueInCallback)
                {
                    //next sound
                    Play(context, sounds, index + 1);
                }
            };
            lock(this)
            {
                player.Play(context, sound);
                this.SoundsPlaying.Add(sound);
                this.PlayersPlaying.Add(player);

                //update our listeners
                ICollection<SoundPlayback> soundsPlaying = new List<SoundPlayback>(this.SoundsPlaying);

                PlaybackChanged?.Invoke(this, new ISoundChainPlaybackService.PlaybackEventArgs(
                    soundsPlaying: soundsPlaying,
                    finished: false,
                    fromStop: false,
                    fromStopGlobal: false
                ));
            }

            if(continueNextSound && !continueInCallback)
            {
                //start a timer to trigger the next sound.
                var timer = new System.Timers.Timer(Math.Max(0, sound.GetActualLength() - sound.Options.ChainDelayMs));
                timer.Elapsed += (tSender, tArgs) =>
                {
                    if (sound.Options.ChainDelayMs < 0 && sound.Options.ChainDelayClip)
                        player.Stop();

                    Play(context, sounds, index + 1);
                };
            }
        }

        public void Stop(bool fromStopGlobal)
        {
            lock(this)
            {
                if (Finished)
                    return;

                Finished = true;

                foreach(var player in PlayersPlaying)
                {
                    player.Stop();
                }
            }

            //update listeners
            PlaybackChanged?.Invoke(this, new ISoundChainPlaybackService.PlaybackEventArgs(
                soundsPlaying: new List<SoundPlayback>(),
                finished: true,
                fromStop: true,
                fromStopGlobal: fromStopGlobal
            ));
        }
    }
}
