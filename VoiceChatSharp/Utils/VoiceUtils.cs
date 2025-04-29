namespace VoiceChatSharp.Utils
{
    public static class VoiceUtils
    {
        static byte[] inputArr;

        /// <summary>
        /// Gets the sample size for a frame.
        /// </summary>
        /// <param name="channels">Set 1 for mono and 2 for stereo</param>
        /// <param name="float32">Float32 size is half</param>
        /// <returns></returns>
        public static int GetSampleSize(int sampleRate, int frameSizeMs, int channels)
        {
            return sampleRate / (1000 / frameSizeMs) * channels;
        }

        /// <summary>
        /// Converts 16 bit PCM data into float 32.
        /// Note that the float array must be half the size of the byte array.
        /// </summary>
        /// <param name="input">The 16 bit PCM data according to your needs.</param>
        /// <param name="output">The output data in which the result will be returned.</param>
        /// modified of https://github.com/realcoloride/OpenVoiceSharp/blob/master/VoiceUtilities.cs
        public static void Convert16BitToFloat(Span<byte> input, Span<float> output)
        {
            if (output.Length * 2 > input.Length)
            {
                throw new ArgumentException("Output span must be half the size of input span");
            }

            for (int i = 0; i < output.Length; i++)
            {
                short sample = (short)((input[i * 2 + 1] << 8) | input[i * 2]);
                output[i] = sample / 32768f;
            }
        }

        /// <summary>
        /// Converts float 32 PCM data into 16 bit.
        /// Note that the byte array must be double the size of the float array.
        /// </summary>
        /// <param name="input">The float 32 PCM data according to your needs.</param>
        /// <param name="output">The output data in which the result will be returned.</param>
        /// <returns>The float32 PCM array.</returns>
        /// https://github.com/realcoloride/OpenVoiceSharp/blob/master/VoiceUtilities.cs
        public static void ConvertFloatTo16Bit(Span<float> input, Span<byte> output)
        {
            int sampleIndex = 0, pcmIndex = 0;

            while (sampleIndex < input.Length)
            {
                short outsample = (short)(input[sampleIndex] * short.MaxValue);
                output[pcmIndex] = (byte)(outsample & 0xff);
                output[pcmIndex + 1] = (byte)((outsample >> 8) & 0xff);

                sampleIndex++;
                pcmIndex += 2;
            }
        }
    }
}
