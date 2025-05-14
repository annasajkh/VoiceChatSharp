using OpusSharp.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.Interfaces
{
    public class VoiceChatAudioSourceInterface
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BytesPerSample { get; set; }
        public float Volume { get; set; }
        public bool Playing { get; set; }

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
            Volume = 1;
            BytesPerSample = bytesPerSample;

            DecodedSamplesPtr = Marshal.AllocHGlobal(Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample));
        }

        public virtual void Play()
        {

        }

        public virtual void Stop()
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
