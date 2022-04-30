namespace Soundbox.Audio
{
    /// <summary>
    /// Audio format for uncompressed Wave audio streams.
    /// </summary>
    public class WaveStreamAudioFormat : StreamAudioFormat
    {
        public override StreamAudioFormatType Type
        {
            get
            {
                return StreamAudioFormatType.Wave;
            }
        }

        public int SampleRate { get; private set; }

        public int BitsPerSample { get; private set; }

        public int ChannelCount { get; private set; }

        #region "Encoding"

        #region "Int"

        public bool IntEncoded { get; private set; }

        public bool IntEncodingSigned { get; private set; }

        #endregion

        #region "Float"

        public bool FloatEncoded { get; private set; }

        #endregion

        public bool ByteOrderLittleEndian { get; private set; }

        #endregion

        protected WaveStreamAudioFormat(int sampleRate, int bitsPerSample, int channelCount, bool intEncoded, bool intEncodingSigned, bool floatEncoded, bool byteOrderLittleEndian)
        {
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            ChannelCount = channelCount;
            IntEncoded = intEncoded;
            IntEncodingSigned = intEncodingSigned;
            FloatEncoded = floatEncoded;
            ByteOrderLittleEndian = byteOrderLittleEndian;
        }

        #region "Static Getters"

        public static WaveStreamAudioFormat GetIntFormat(int sampleRate, int bitsPerSample, int channelCount, bool signed = true, bool littleEndian = true)
        {
            return new WaveStreamAudioFormat(
                sampleRate: sampleRate,
                bitsPerSample: bitsPerSample,
                channelCount: channelCount,
                intEncoded: true,
                intEncodingSigned: signed,
                floatEncoded: false,
                byteOrderLittleEndian: littleEndian
            );
        }

        public static WaveStreamAudioFormat GetFloatFormat(int sampleRate, int bitsPerSample, int channelCount, bool littleEndian = true)
        {
            return new WaveStreamAudioFormat(
                sampleRate: sampleRate,
                bitsPerSample: bitsPerSample,
                channelCount: channelCount,
                intEncoded: false,
                intEncodingSigned: false,
                floatEncoded: true,
                byteOrderLittleEndian: littleEndian
            );
        }

        #endregion

        #region "ToString"

        public override string ToString()
        {
            string str = "Wave{";
            if (FloatEncoded)
            {
                str += "Float," + BitsPerSample;
            }
            else if (IntEncoded)
            {
                str += "Int," + BitsPerSample + "," + (IntEncodingSigned ? "Signed" : "Unsigned");
            }

            str += "," + (ByteOrderLittleEndian ? "LittleEndian" : "BigEndian") + "}";

            return str;
        }

        #endregion
    }
}
