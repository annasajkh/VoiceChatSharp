using Hexa.NET.SDL3;
using OpusSharp.Core;
using System;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
    {
        public override void Init(int sampleRate, int channels, int bytesPerSample, int frameSizeMS, OpusDecoder opusDecoder)
        {
            base.Init(sampleRate, channels, bytesPerSample, frameSizeMS, opusDecoder);

            SDLAudioSpec sdlAudioSpec = new SDLAudioSpec();
            sdlAudioSpec.Format = SDLAudioFormat.F32;
            sdlAudioSpec.Freq = SampleRate;
            sdlAudioSpec.Channels = Channels;
        }

        public override void Update()
        {
            unsafe
            {
                Span<float> decodedSamples = new Span<float>((void*)DecodedSamplesPtr, Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample / sizeof(float)));

                // mixes into itself to just change the volume
                SDL.MixAudio((byte*)DecodedSamplesPtr, (byte*)DecodedSamplesPtr, SDLAudioFormat.F32, (uint)Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample), Volume);

                DecodedSamplesQueue.Enqueue(decodedSamples.ToArray());
            }
        }

        public override void Play()
        {
            Playing = true;
        }

        public override void Pause()
        {
            Playing = false;
        }

        public override void SetVolume(float volume)
        {
            Volume = volume;
        }
    }
}
