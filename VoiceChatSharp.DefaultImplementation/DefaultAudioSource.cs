using MiniAudioEx;
using System;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultAudioSource : AudioSourceInterface
{
    public AudioSource AudioSource { get; private set; }

    public DefaultAudioSource(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
    {
        AudioContext.Initialize((uint)SampleRate, (uint)Channels);

        AudioSource = new AudioSource();
            
        AudioSource.Read += (AudioBuffer<float> framesOut, ulong frameCount, int channels) =>
        {
            Span<float> framesSpan;

            unsafe
            {
                framesSpan = new Span<float>((void*)framesOut.Pointer, (int)frameCount);
            }

            OnAudioReadInternal(framesSpan);
        };
    }

    /// <summary>
    /// Play the audio source.
    /// </summary>
    public override void Play()
    {
        AudioSource.Play();
    }

    /// <summary>
    /// Stop the audio source.
    /// </summary>
    public override void Stop()
    {
        AudioSource.Stop();
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public override void Dispose()
    {
        AudioSource.Dispose();
    }
}