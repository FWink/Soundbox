using IrrKlang;
using Soundbox.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Soundbox.Playback.IrrKlang
{
    public class IrrKlangSoundPlaybackService : ISoundPlaybackService, ISoundPlaybackVirtualVolumeService, ISoundStopEventReceiver
    {
        public event EventHandler<ISoundPlaybackService.PlaybackEventArgs> PlaybackFinished;

        protected readonly IIrrKlangEngineProvider EngineProvider;
        protected readonly IServiceProvider ServiceProvider;

        /// <summary>
        /// Playback requested in <see cref="Play(SoundboxContext, SoundPlayback)"/>.
        /// </summary>
        protected SoundPlayback Sound;

        /// <summary>
        /// Indicates that playback has finished either because the sound completed playing or because <see cref="Stop"/> was called.
        /// </summary>
        protected bool Finished = false;

        /// <summary>
        /// The playback handle that we can use to control the sound playback (e.g. stop and modify volume).
        /// </summary>
        protected ISound Playback;

        public IrrKlangSoundPlaybackService(IIrrKlangEngineProvider engineProvider, IServiceProvider serviceProvider)
        {
            this.EngineProvider = engineProvider;
            this.ServiceProvider = serviceProvider;
        }

        public async Task Play(SoundboxContext context, SoundPlayback sound)
        {
            if (Finished)
                return;

            this.Sound = sound;
            var engine = await EngineProvider.GetSoundEngine();
            Playback = engine.Play2D(context.GetAbsoluteFileName(sound.Sound));

            //set playback options
            Playback.PlaybackSpeed = (float)sound.Options.SpeedPitch;

            double volume = sound.Options.Volume;

            //try to connect to a virtual volume service if any
            var virtualVolumeService = ServiceProvider.GetService(typeof(IVirtualVolumeService)) as IVirtualVolumeService;
            if (virtualVolumeService != null)
            {
                volume = Volume.GetVolume(volume, await virtualVolumeService.GetVolume());
                virtualVolumeService.RegisterSoundPlayback(this);
            }

            Playback.Volume = Utilities.GetVolume(volume);

            //notify when the sound is done playing
            Playback.setSoundStopEventReceiver(this);
        }

        public void SetVolume(double volume)
        {
            if (Finished)
                return;

            Playback.Volume = Utilities.GetVolume(Volume.GetVolume(this.Sound.Options.Volume, volume));
        }

        public void Stop()
        {
            lock (this)
            {
                if (Finished)
                    return;
                Finished = true;
            }

            Playback?.Stop();
            Playback?.Dispose();

            //notify listeners
            PlaybackFinished?.Invoke(this, new ISoundPlaybackService.PlaybackEventArgs(
                sound: this.Sound,
                fromStop: true
            ));
        }

        public void OnSoundStopped(ISound sound, StopEventCause reason, object userData)
        {
            lock (this)
            {
                if (Finished)
                    return;
                Finished = true;
            }
            //this throws an error
            //TODO make sure the sound is disposed eventually
            //sound.Dispose();

            PlaybackFinished?.Invoke(this, new ISoundPlaybackService.PlaybackEventArgs(
                sound: this.Sound,
                fromStop: false
            ));
        }
    }
}
