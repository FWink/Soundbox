using System.Threading.Tasks;

namespace Soundbox.Audio.Processing
{
    /// <summary>
    /// For <see cref="IStreamAudioSource"/>s that wrap an audio stream and modify its output.
    /// By default, all methods if <see cref="IStreamAudioSource"/> call through to <see cref="WrappedAudioSource"/>
    /// </summary>
    public interface IWrappedStreamAudioSource : IStreamAudioSource
    {
        IStreamAudioSource WrappedAudioSource { get; }

        WaveStreamAudioFormat IStreamAudioSource.Format => WrappedAudioSource.Format;

        Task IStreamAudioSource.Start()
        {
            return WrappedAudioSource.Start();
        }

        Task IStreamAudioSource.Stop()
        {
            return WrappedAudioSource.Stop();
        }

        void System.IDisposable.Dispose()
        {
            WrappedAudioSource?.Dispose();
        }

        event System.EventHandler<StreamAudioSourceDataEvent> IStreamAudioSource.DataAvailable
        {
            add { WrappedAudioSource.DataAvailable += value; }
            remove { WrappedAudioSource.DataAvailable -= value; }
        }

        event System.EventHandler<StreamAudioSourceStoppedEvent> IStreamAudioSource.Stopped
        {
            add { WrappedAudioSource.Stopped += value; }
            remove { WrappedAudioSource.Stopped -= value; }
        }
    }
}
