using OpusSharp.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.Interfaces
{
    public class VoiceChatAudioSourceInterface
    {
        public int SampleRate { get; protected set; }
        public int Channels { get; protected set; }
        public int BytesPerSample { get; protected set; }
        public bool Playing { get; protected set; }
        public float Volume { get; protected set; }

        public unsafe IntPtr DecodedSamplesPtr { get; private set; }

        public OpusDecoder OpusDecoder { get; set; }

        /// <summary>
        /// Contains encoded samples encoded by opus
        /// </summary>
        public ConcurrentQueue<byte[]> EncodedSamplesQueue { get; private set; } = new();
        public ConcurrentQueue<float[]> DecodedSamplesQueue { get; private set; } = new();

        bool isDisposed;

        /// <summary>
        /// Initialize the audio source.
        /// </summary>
        public virtual void Init(int sampleRate, int channels, int bytesPerSample, OpusDecoder opusDecoder)
        {
            SampleRate = sampleRate;
            Channels = channels;
            OpusDecoder = opusDecoder;
            BytesPerSample = bytesPerSample;
            Volume = 1;

            DecodedSamplesPtr = Marshal.AllocHGlobal(Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample));
        }


        /// <summary>
        /// Set volume for voice chat audio source.
        /// </summary>
        public virtual void SetVolume(float volume)
        {

        }

        public virtual void Play()
        {

        }

        public virtual void Pause()
        {

        }

        /// <summary>
        /// Implement this in the backend
        /// </summary>
        public virtual void Update()
        {

        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public virtual void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            if (DecodedSamplesPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(DecodedSamplesPtr);
                DecodedSamplesPtr = IntPtr.Zero;
            }
        }
    }
}
