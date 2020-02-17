using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Plays a single sound (<see cref="SoundPlayback"/>). One instance should only be used to play one sound.
    /// </summary>
    public interface ISoundPlaybackService
    {
        /// <summary>
        /// Asynchronously starts the playback of the given sound.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="sound"></param>
        void Play(SoundboxContext context, SoundPlayback sound);

        /// <summary>
        /// Immediately stops the playback of the sound.
        /// </summary>
        void Stop();

        /// <summary>
        /// Called when the playback of the sound is finished; either because it ended naturally or because <see cref="Stop"/> has been called.
        /// </summary>
        event EventHandler<PlaybackEventArgs> PlaybackFinished;

        public class PlaybackEventArgs : SoundPlaybackEventArgs
        {
            /// <summary>
            /// True if the playback state changed because <see cref="Stop"/> was called.
            /// </summary>
            public readonly bool FromStop;

            public PlaybackEventArgs(SoundPlayback sound, bool fromStop) : base(sound)
            {
                this.FromStop = fromStop;
            }
        }
    }
}
