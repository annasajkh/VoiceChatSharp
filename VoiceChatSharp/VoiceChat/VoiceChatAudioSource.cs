using System.Runtime.InteropServices;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.VoiceChat
{
    public class VoiceChatAudioSource : IDisposable
    {
        public VoiceChatAudioSourceInterface VoiceChatAudioSourceInterface { get; private set; }

        public VoiceChatAudioSource(VoiceChatAudioSourceInterface voiceChatAudioSourceInterface)
        {
            VoiceChatAudioSourceInterface = voiceChatAudioSourceInterface;
        }

        public void EnqueueEncodedSample(byte[] encodedSample)
        {
            VoiceChatAudioSourceInterface.EncodedSamplesQueue.Enqueue(encodedSample);
        }


        /// <summary>
        /// Set the volume of the audio source
        /// </summary>
        /// <param name="volume">The volume</param>
        public void SetVolume(float volume)
        {
            VoiceChatAudioSourceInterface.Volume = volume;
        }

        /// <summary>
        /// Play the audio source
        /// </summary>
        public void Play()
        {
            VoiceChatAudioSourceInterface.Playing = true;
        }

        public void Update()
        {
            if (!VoiceChatAudioSourceInterface.EncodedSamplesQueue.TryDequeue(out byte[]? samples))
            {
                return;
            }

            Span<float> decodedSamples;

            int totalSamplesBytes = Helper.GetTotalBytes(VoiceChatAudioSourceInterface.SampleRate, Global.FrameSizeMs, VoiceChatAudioSourceInterface.Channels, VoiceChatAudioSourceInterface.BytesPerSample);

            unsafe
            {
                lock (VoiceChatAudioSourceInterface.DecodedSamplesPtrLock)
                {
                    decodedSamples = new Span<float>((void*)VoiceChatAudioSourceInterface.DecodedSamplesPtr, totalSamplesBytes / sizeof(float));
                }
            }

            VoiceChatAudioSourceInterface.OpusDecoder.Decode(samples, samples.Length, decodedSamples, totalSamplesBytes / sizeof(float) / VoiceChatAudioSourceInterface.Channels, false);
            VoiceChatAudioSourceInterface.Update();
        }

        /// <summary>
        /// Pause the audio source
        /// </summary>
        public void Pause()
        {
            VoiceChatAudioSourceInterface.Playing = false;
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public void Dispose()
        {

            lock (VoiceChatAudioSourceInterface.DecodedSamplesPtrLock)
            {
                Marshal.FreeHGlobal(VoiceChatAudioSourceInterface.DecodedSamplesPtr);
            }

            VoiceChatAudioSourceInterface.Dispose();
        }
    }
}
