using System;
using System.Collections;
using System.Collections.Generic;

namespace Soundbox.Audio
{
    /// <summary>
    /// A collection of uncompressed audio samples (represented by an <see cref="ArraySegment{T}"/>).
    /// </summary>
    public class SampleCollection : IEnumerable<ArraySegment<byte>>
    {
        public WaveStreamAudioFormat Format { get; private set; }

        protected readonly ArraySegment<byte> Buffer;

        /// <summary>
        /// Number of bytes per sample.
        /// </summary>
        protected readonly int SampleByteCount;

        public SampleCollection(WaveStreamAudioFormat format, ArraySegment<byte> buffer)
        {
            this.Format = format;
            this.Buffer = buffer;
            this.SampleByteCount = format.BitsPerSample / 8;
        }

        /// <summary>
        /// Number of samples in this collection.
        /// </summary>
        public int Count => Buffer.Count / SampleByteCount;

        /// <summary>
        /// Returns the sample at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ArraySegment<byte> this[int index] => new ArraySegment<byte>(Buffer.Array, Buffer.Offset + index * SampleByteCount, SampleByteCount);

        public IEnumerator<ArraySegment<byte>> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
