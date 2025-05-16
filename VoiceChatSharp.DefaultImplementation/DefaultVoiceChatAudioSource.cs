using Hexa.NET.SDL3;
using OpusSharp.Core;
using System;
using System.Runtime.InteropServices;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
    {
        public unsafe SDLAudioStream* AudioStream { get; private set; }

        public override void Init(int sampleRate, int channels, int bytesPerSample, int frameSizeMS, OpusDecoder opusDecoder)
        {
            base.Init(sampleRate, channels, bytesPerSample, frameSizeMS, opusDecoder);

            SDLAudioSpec sdlAudioSpec = new SDLAudioSpec();
            sdlAudioSpec.Format = SDLAudioFormat.F32;
            sdlAudioSpec.Freq = SampleRate;
            sdlAudioSpec.Channels = Channels;

            unsafe
            {
                AudioStream = SDL.CreateAudioStream(&sdlAudioSpec, (SDLAudioSpec*)IntPtr.Zero);
            }
        }

        public override bool IsAudioDeviceWantSamples()
        {
            unsafe
            {
                if (SDL.GetAudioStreamQueued(AudioStream) >= Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample))
                {
                    return false;
                }
            }

            return true;
        }

        public override void Update()
        {
            unsafe
            {
                if (!SDL.PutAudioStreamData(AudioStream, (void*)DecodedSamplesPtr, Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample)))
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    Logger.LogWarning($"Cannot put audio stream data SDL_Error: {errorMessage}");
                }
            }
        }

        public override void Play()
        {
            Playing = true;

            unsafe
            {
                SDL.ResumeAudioStreamDevice(AudioStream);
            }
        }

        public override void Pause()
        {
            Playing = false;

            unsafe
            {
                SDL.PauseAudioStreamDevice(AudioStream);
            }
        }

        public override void SetVolume(float volume)
        {
            Volume = volume;

            unsafe
            {
                SDL.SetAudioStreamGain(AudioStream, Volume);
            }
        }
    }
}
