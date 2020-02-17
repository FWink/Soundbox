using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox
{
    /// <summary>
    /// Plays an entire chain of sounds (<see cref="SoundPlaybackRequest"/> via <see cref="ISoundPlaybackService"/>.
    ///  One instance should only be used to play one sound chain.
    /// </summary>
    public interface ISoundChainPlaybackService
    {
        /// <summary>
        /// Asynchronously starts the playback of the given sound chain.
        /// Use <see cref="PlaybackChanged"/> to stay informed of the current state of playback.
        /// </summary>
        /// <param name="context">Current execution context</param>
        /// <param name="sounds"></param>
        void Play(SoundboxContext context, SoundPlaybackRequest sounds);

        /// <summary>
        /// Stops playback of the entire chain immediately.
        /// </summary>
        /// <param name="fromStopGlobal">
        /// True if Stop is being called on this chain because the playback of sounds is stopped globally by a user.
        /// <see cref="PlaybackEventArgs.FromStopGlobal"/>
        /// </param>
        void Stop(bool fromStopGlobal);

        /// <summary>
        /// Called whenever the current playback state of the chain changes (e.g. a sound finishes playback or <see cref="Stop"/> is called).
        /// </summary>
        event EventHandler<PlaybackEventArgs> PlaybackChanged;

        public class PlaybackEventArgs : EventArgs
        {
            /// <summary>
            /// True if the chain is fully done playing; either because it ended naturally or because <see cref="Stop(bool)"/> was called.
            /// </summary>
            public readonly bool Finished;

            /// <summary>
            /// True if the playback state changed because <see cref="Stop"/> was called.
            /// </summary>
            public readonly bool FromStop;

            /// <summary>
            /// True if <see cref="FromStop"/> is true because the playback of sounds had been stopped globally by a user.
            /// </summary>
            public readonly bool FromStopGlobal;

            /// <summary>
            /// The sounds that are currently being played.
            /// </summary>
            public readonly ICollection<SoundPlayback> SoundsPlaying;

            public PlaybackEventArgs(ICollection<SoundPlayback> soundsPlaying, bool finished, bool fromStop, bool fromStopGlobal)
            {
                SoundsPlaying = soundsPlaying;
                Finished = finished;
                FromStop = fromStop;
                FromStopGlobal = fromStopGlobal;
            }
        }
    }
}
