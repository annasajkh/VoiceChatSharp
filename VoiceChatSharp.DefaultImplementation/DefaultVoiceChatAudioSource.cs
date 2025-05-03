using Miniaudio;
using OpusSharp.Core;
using System;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
    {

        public override void Init(int sampleRate, int channels, int bytesPerSample, OpusDecoder opusDecoder)
        {
            base.Init(sampleRate, channels, bytesPerSample, opusDecoder);
        }

        public override void Update()
        {
            lock (DecodedSamplesPtrLock)
            {
                if (DecodedSamplesPtr == IntPtr.Zero)
                {
                    return;
                }
            }

            Span<float> decodedSamples;

            unsafe
            {
                lock (DecodedSamplesPtrLock)
                {
                    decodedSamples = new Span<float>((void*)DecodedSamplesPtr, Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample) / sizeof(float));
                }
            }

            unsafe
            {
                lock (DecodedSamplesPtrLock)
                {
                    ma.apply_volume_factor_pcm_frames_f32((float*)DecodedSamplesPtr, (ulong)(Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample) / sizeof(float)), (uint)Channels, Volume);
                }
            }

            if (Playing)
            {
                DecodedSamplesQueue.Enqueue(decodedSamples.ToArray());
            }
        }

        public override void Dispose()
        {

        }
    }
}
