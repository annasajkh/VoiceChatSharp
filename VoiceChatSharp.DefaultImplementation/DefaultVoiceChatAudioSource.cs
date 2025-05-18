using Hexa.NET.SDL3;
using OpusSharp.Core;
using System;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
{
    public unsafe SDLAudioStream* AudioStream;

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

    public override void Update()
    {
        unsafe
        {
            Span<float> decodedSamples = new Span<float>((void*)DecodedSamplesPtr, Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample / sizeof(float)));
            DecodedSamplesQueue.Enqueue(decodedSamples.ToArray());
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

    public override void Dispose()
    {
        base.Dispose();

        if (isDisposed)
        {
            return;
        }

        unsafe
        {
            SDL.DestroyAudioStream(AudioStream);
        }
    }
}
