using VoiceChatSharp.Interfaces;
using SDL;
using System;
using System.Threading;
using System.Runtime.InteropServices;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultRecorder : RecorderInterface
{
    unsafe SDL_AudioStream* audioStream;
    Thread pollingThread;
    bool isRecording;
    SDL_AudioDeviceID deviceId;

    int samplesPerFrame;

    public DefaultRecorder(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
    {
        if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_AUDIO))
        {
            throw new Exception($"Cannot init sdl audio: {SDL3.SDL_GetError()}");
        }

        int frameSizeMs = 20;
        samplesPerFrame = SampleRate * frameSizeMs / 1000;

        SDL_AudioSpec audioSpec = new();
        audioSpec.format = SDL3.SDL_AUDIO_F32;
        audioSpec.freq = SampleRate;
        audioSpec.channels = Channels;

        unsafe
        {
            audioStream = SDL3.SDL_OpenAudioDeviceStream(SDL3.SDL_AUDIO_DEVICE_DEFAULT_RECORDING, &audioSpec, null, nint.Zero);

            if (audioStream == null)
            {
                throw new Exception($"Failed to open audio device: {SDL3.SDL_GetError()}");
            }

            deviceId = SDL3.SDL_GetAudioStreamDevice(audioStream);
        }

    }

    public override void StartRecording()
    {
        unsafe
        {
            SDL3.SDL_ResumeAudioDevice(deviceId);
        }

        isRecording = true;
        pollingThread = new Thread(PollAudio);
        pollingThread.Start();
    }

    private unsafe void PollAudio()
    {
        nint bufferPtr = (nint)NativeMemory.Alloc((nuint)(samplesPerFrame * sizeof(float)));

        while (isRecording)
        {
            if (SDL3.SDL_GetAudioStreamAvailable(audioStream) != 0)
            {
                int bytesRead = SDL3.SDL_GetAudioStreamData(audioStream, bufferPtr, samplesPerFrame * sizeof(float));

                if (bytesRead > 0)
                {
                    Span<float> rawAudioData = new Span<float>((void*)bufferPtr, samplesPerFrame);
                    OnAudioReadInternal(rawAudioData);
                }
            }
        }

        NativeMemory.Free((void*)bufferPtr);
    }

    public override void StopRecording()
    {
        isRecording = false;
        pollingThread?.Join();

        unsafe
        {
            SDL3.SDL_ResumeAudioDevice(deviceId);
        }
    }

    public override void Dispose()
    {
        isRecording = false;
        pollingThread?.Join();

        unsafe
        {
            SDL3.SDL_ResumeAudioDevice(deviceId);
        }
    }
}