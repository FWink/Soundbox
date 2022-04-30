using System;
using System.Threading.Tasks;

namespace Soundbox.Audio.Processing.Noisegate.Implementation
{
    /// <summary>
    /// Default noise gate implementation that looks at peak amplitudes per interval and is compatible
    /// with float and int encodings.
    /// </summary>
    public class DefaultNoiseGateStreamAudioProcessor : INoiseGateStreamAudioProcessor
    {
        public IStreamAudioSource WrappedAudioSource { get; private set; }

        protected NoiseGateStreamAudioProcessorOptions Options { get; private set; }

        public void SetAudioSource(IStreamAudioSource source)
        {
            this.WrappedAudioSource = source;
            source.DataAvailable += (s, e) => OnSourceDataAvailable(e);
        }

        public Task SetOptions(NoiseGateStreamAudioProcessorOptions options)
        {
            this.Options = options;
            return Task.CompletedTask;
        }

        public event EventHandler<StreamAudioSourceDataEvent> DataAvailable;

        /// <summary>
        /// Called when <see cref="WrappedAudioSource"/> raises <see cref="IStreamAudioSource.DataAvailable"/>.
        /// Processing is happening here: checks on the volume of the received samples and passes the event on to <see cref="DataAvailable"/> if appropriate.
        /// </summary>
        /// <param name="data"></param>
        protected void OnSourceDataAvailable(StreamAudioSourceDataEvent data)
        {
            var options = this.Options;

            bool overThreshold = false;
            foreach (var sample in data.Samples)
            {
                var amplitude = GetNormalizedSampleValue(sample, data.Format);
                if (amplitude > options.VolumeThreshold)
                {
                    overThreshold = true;
                    break;
                }
            }

            //TODO noisegate. super simple noise gate for now: per event only
            if (!overThreshold)
            {
                if (options.ModeCutOff)
                    //simply do nothing
                    return;

                //return a zeroed buffer
                byte[] zeros = new byte[data.BytesAvailable];
                DataAvailable?.Invoke(this, new StreamAudioSourceDataEvent()
                {
                    Buffer = zeros,
                    BytesAvailable = zeros.Length,
                    Format = data.Format
                });
            }
            else
            {
                //pass through
                DataAvailable?.Invoke(this, data);
            }
        }

        /// <summary>
        /// Reads one sample from the given buffer and maps it to the range of <see cref="NoiseGateStreamAudioProcessorOptions.VolumeThreshold"/>.
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static float GetNormalizedSampleValue(ArraySegment<byte> sample, WaveStreamAudioFormat format)
        {
            sample = GetSampleBytes(sample, format);

            if (format.FloatEncoded)
            {
                if (format.BitsPerSample == 32)
                {
                    float f = BitConverter.ToSingle(sample);
                    //1 = 0dB = highest volume without distortion
                    return Math.Abs(f) * 100;
                }

                throw new ArgumentException($"Unsupported float encoding: BitsPerSample={format.BitsPerSample}");
            }
            else if (format.IntEncoded)
            {
                uint value;
                uint max;

                if (format.BitsPerSample == 32)
                {
                    max = int.MaxValue;

                    if (format.IntEncodingSigned)
                    {
                        int i = BitConverter.ToInt32(sample);
                        if (i == int.MinValue)
                            ++i;
                        value = (uint)Math.Abs(i);
                    }
                    else
                    {
                        value = BitConverter.ToUInt32(sample);
                    }
                }
                else if (format.BitsPerSample == 16)
                {
                    max = (uint)short.MaxValue;

                    if (format.IntEncodingSigned)
                    {
                        short i = BitConverter.ToInt16(sample);
                        if (i == short.MinValue)
                            ++i;
                        value = (uint)Math.Abs(i);
                    }
                    else
                    {
                        value = BitConverter.ToUInt16(sample);
                    }
                }
                else if (format.BitsPerSample == 8)
                {
                    max = (uint)sbyte.MaxValue;

                    if (format.IntEncodingSigned)
                    {
                        int i = sample[0];
                        if (i == sbyte.MinValue)
                            ++i;
                        value = (uint)Math.Abs(i);
                    }
                    else
                    {
                        value = sample[0];
                    }
                }
                else if (format.BitsPerSample == 24)
                {
                    max = 16777215;

                    int i;
                    if (BitConverter.IsLittleEndian)
                        i = (sample[2] << 16) | (sample[1] << 8) | sample[0];
                    else
                        i = (sample[0] << 16) | (sample[1] << 8) | sample[2];

                    if (format.IntEncodingSigned)
                    {
                        if ((i & (1 << 24)) != 0)
                        {
                            //is negative
                            i |= 0xFF;
                        }
                        value = (uint)Math.Abs(i);
                    }
                    else
                    {
                        value = (uint)i;
                    }
                }
                else
                {
                    throw new ArgumentException($"Unsupported int encoding: BitsPerSample={format.BitsPerSample}, Signed={format.IntEncodingSigned}");
                }

                if (!format.IntEncodingSigned)
                {
                    //max == middle currently. shift all values so that a "middle" sample = 0 = min value
                    if (value < max)
                        value = max + (max - value);
                    value -= max;
                    if (value >= max)
                        value = max;
                }

                //100 must be a double. the mantissa of a float isn't large enough, thus we might get some unexpected results when value is close to max
                return (float)(value / (max / 100.0));
            }

            throw new ArgumentException($"Unsupported wave encoding: neither int nor float");
        }

        /// <summary>
        /// Returns the sample with endianness correction if required:
        /// the returned endianness matches <see cref="BitConverter.IsLittleEndian"/>.
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static ArraySegment<byte> GetSampleBytes(ArraySegment<byte> sample, WaveStreamAudioFormat format)
        {
            if (format.ByteOrderLittleEndian == BitConverter.IsLittleEndian)
                return sample;

            //copy and reverse the bytes
            int byteCount = format.BitsPerSample / 8;

            byte[] sampleReversed = new byte[byteCount];
            for (int i = 0; i < byteCount; ++i)
            {
                sampleReversed[i] = sample[byteCount - i - 1];
            }

            return sampleReversed;
        }
    }
}
