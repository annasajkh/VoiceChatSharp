using SDL;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultAudioSource : AudioSourceInterface
{
    unsafe SDL_AudioStream* audioStream;
    Thread pollingThread;
    bool isPlaying;
    SDL_AudioDeviceID deviceId;

    int samplesPerFrame;


    public DefaultAudioSource(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
    {
        int frameSizeMs = 20;
        samplesPerFrame = SampleRate * frameSizeMs / 1000;

        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO))
        {
            throw new Exception($"Cannot init sdl audio: {SDL3.SDL_GetError()}");
        }

        SDL_AudioSpec audioSpec = new();
        audioSpec.format = SDL3.SDL_AUDIO_F32;
        audioSpec.freq = SampleRate;
        audioSpec.channels = Channels;

        unsafe
        {
            audioStream = SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK, &audioSpec, null, nint.Zero);

            if (audioStream == null)
            {
                throw new Exception($"Failed to open audio device: {SDL3.SDL_GetError()}");
            }

            deviceId = SDL3.SDL_GetAudioStreamDevice(audioStream);
        }
    }

    /// <summary>
    /// Play the audio source.
    /// </summary>
    public override void Play()
    {
        unsafe
        {
            SDL3.SDL_ResumeAudioDevice(deviceId);
        }

        isPlaying = true;
        pollingThread = new Thread(PollAudio);
        pollingThread.Start();
    }

    private void PollAudio()
    {
        unsafe
        {
            nint bufferPtr = (nint)NativeMemory.Alloc((nuint)(samplesPerFrame * sizeof(float)));

            while (isPlaying)
            {
                int minimumAudio = (samplesPerFrame * sizeof(float));

                if (SDL3.SDL_GetAudioStreamQueued(audioStream) < minimumAudio)
                {
                    OnAudioReadInternal(new Span<float>((void*)bufferPtr, samplesPerFrame));

                    SDLBool canPutAudioStreamData = SDL3.SDL_PutAudioStreamData(audioStream, (nint)bufferPtr, samplesPerFrame * sizeof(float));

                    if (!canPutAudioStreamData)
                    {
                        Logger.LogWarning("Warning cannot send audio stream data to sdl");
                    }
                }
            }

            NativeMemory.Free((void*)bufferPtr);
        }
    }

    /// <summary>
    /// Stop the audio source.
    /// </summary>
    public override void Stop()
    {
        isPlaying = false;
        pollingThread?.Join();

        unsafe
        {
            SDL3.SDL_PauseAudioDevice(deviceId);
        }
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public override void Dispose()
    {
        isPlaying = false;
        pollingThread?.Join();

        unsafe
        {
            SDL3.SDL_CloseAudioDevice(deviceId);
        }
    }
}