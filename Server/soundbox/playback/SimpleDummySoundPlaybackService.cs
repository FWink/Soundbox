using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// A simple, dummy-akin player that can player .wav files only without any effects.
    /// </summary>
    public class SimpleDummySoundPlaybackService : ISoundPlaybackService
    {
        /// <summary>
        /// True: playback has finished either naturally or by calling <see cref="Stop"/>.
        /// </summary>
        protected bool Finished = false;

        protected SoundPlayback Sound;

        protected System.Media.SoundPlayer WavPlayer;

        public event EventHandler<ISoundPlaybackService.PlaybackEventArgs> PlaybackFinished;

        public Task Play(SoundboxContext context, SoundPlayback sound)
        {
            if (Finished)
                return Task.FromResult(false);

            this.Sound = sound;
            this.WavPlayer = new System.Media.SoundPlayer(context.GetAbsoluteFileName(sound.Sound));

            new System.Threading.Thread(() =>
            {
                WavPlayer.PlaySync();
                lock (this)
                {
                    if (Finished)
                        return;
                    Finished = true;

                    PlaybackFinished?.Invoke(this, new ISoundPlaybackService.PlaybackEventArgs(
                        sound: sound,
                        fromStop: false
                    ));
                }
            }).Start();

            return Task.FromResult(true);
        }

        public void Stop()
        {
            lock (this)
            {
                if (Finished)
                    return;
                Finished = true;
            }

            if (WavPlayer != null)
                WavPlayer.Stop();

            PlaybackFinished?.Invoke(this, new ISoundPlaybackService.PlaybackEventArgs(
                sound: this.Sound,
                fromStop: true
            ));
        }
    }
}
