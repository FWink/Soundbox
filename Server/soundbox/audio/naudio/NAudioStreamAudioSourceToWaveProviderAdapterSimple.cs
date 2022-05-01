using NAudio.Wave;
using System;

namespace Soundbox.Audio.NAudio
{
    /// <summary>
    /// Adapter to turn the output of a <see cref="IStreamAudioSource"/> into a <see cref="IWaveProvider"/>.<br/>
    /// <see cref="Write(StreamAudioSourceDataEvent)"/> must be called from <see cref="IStreamAudioSource.DataAvailable"/>.
    /// Note that this class does no buffering of its own. Thus, <see cref="Read(byte[], int, int)"/> must be fully consumed within
    /// the <see cref="IStreamAudioSource.DataAvailable"/> event.
    /// </summary>
    public class NAudioStreamAudioSourceToWaveProviderAdapterSimple : IWaveProvider
    {
        protected StreamAudioSourceDataEvent Data;
        protected int DataOffset;

        public NAudioStreamAudioSourceToWaveProviderAdapterSimple(IStreamAudioSource source)
        {
            this.WaveFormat = NAudioUtilities.ToNAudioWaveFormat(source.Format);
        }

        /// <summary>
        /// Writes a data event into the adapter. The given event's buffer is not copied, but stored as reference.
        /// <see cref="Read(byte[], int, int)"/> must be called immediately to consume this event. Otherwise the event's buffer might become invalid.
        /// </summary>
        /// <param name="dataEvent"></param>
        public void Write(StreamAudioSourceDataEvent dataEvent)
        {
            this.Data = dataEvent;
            DataOffset = 0;
        }

        #region "IWaveProvider"
        public WaveFormat WaveFormat { get; private set; }

        public int Read(byte[] buffer, int offset, int count)
        {
            count = Math.Min(count, Data.Buffer.Count - DataOffset);
            if (count <= 0)
                return 0;

            //copy into target buffer
            Array.Copy(Data.Buffer.Array, Data.Buffer.Offset + DataOffset, buffer, offset, count);
            DataOffset += count;

            return count;
        }
        #endregion
    }
}
