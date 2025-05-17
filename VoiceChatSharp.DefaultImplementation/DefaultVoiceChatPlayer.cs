using Hexa.NET.SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatPlayer : VoiceChatPlayerInterface
    {
        uint logicalDeviceID;
        uint physicalDeviceID;

        Dictionary<string, uint> audioDevicesMapping = new();
        List<DefaultVoiceChatAudioSource> defaultVoiceChatAudioSources = new();

        bool alreadyInitialized;
        bool isDisposed;

        float[] mixedSample;

        public unsafe SDLAudioStream* audioStream { get; private set; }

        public DefaultVoiceChatPlayer(int sampleRate = 48000, int channels = 2, string? playingDevice = null) : base(sampleRate, channels, 4) // 4 for 32 bit f32 it's 4 bytes
        {
            if (!SDL.Init(SDLInitFlags.Audio))
            {
                unsafe
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot initialize sdl audio SDL_Error: {errorMessage}");
                }
            }

            FrameSizeMS = 20;

            int sampleFrames = Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample) / Channels / sizeof(float);

            if (!SDL.SetHintWithPriority(SDL.SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, $"{sampleFrames}", SDLHintPriority.Override))
            {
                unsafe
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());

                    throw new Exception($"cannot set SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES SDL_Error: {errorMessage}");
                }
            }

            SDLAudioSpec sdlAudioSpec = new SDLAudioSpec();
            sdlAudioSpec.Format = SDLAudioFormat.F32;
            sdlAudioSpec.Freq = SampleRate;
            sdlAudioSpec.Channels = Channels;

            unsafe
            {
                audioStream = SDL.CreateAudioStream(&sdlAudioSpec, (SDLAudioSpec*)IntPtr.Zero);
            }

            InitDevice(sampleRate, channels, playingDevice);

            mixedSample = new float[Helper.GetTotalBytes(sampleRate, FrameSizeMS, Channels, BytesPerSample) / sizeof(float)];
        }

        public override void Update()
        {
            int totalSamplesBytes = Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample);

            unsafe
            {
                if (SDL.GetAudioStreamQueued(audioStream) >= totalSamplesBytes)
                {
                    return;
                }
            }

            foreach (VoiceChatAudioSource VoiceChatAudioSource in VoiceChatAudioSources.Values)
            {
                if (!VoiceChatAudioSource.VoiceChatAudioSourceInterface.DecodedSamplesQueue.TryDequeue(out float[] decodedSample))
                {
                    continue;
                }

                unsafe
                {
                    fixed (float* mixedSamplePtr = mixedSample)
                    fixed (float* decodedSamplePtr = decodedSample)
                    {
                        SDL.MixAudio((byte*)mixedSamplePtr, (byte*)decodedSamplePtr, SDLAudioFormat.F32, (uint)(totalSamplesBytes), Volume);
                    }
                }
            }

            unsafe
            {
                fixed (float* mixedSamplePtr = mixedSample)
                {
                    if (!SDL.PutAudioStreamData(audioStream, mixedSamplePtr, totalSamplesBytes))
                    {
                        string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                        Logger.LogWarning($"Cannot put audio stream data SDL_Error: {errorMessage}");
                    }
                }
            }

            Array.Clear(mixedSample, 0, mixedSample.Length);
        }

        void InitDevice(int sampleRate, int channels, string? playbackDevice = null)
        {
            unsafe
            {
                if (alreadyInitialized)
                {
                    SDL.CloseAudioDevice(logicalDeviceID);
                }

                SDLAudioSpec sdlAudioSpec = new SDLAudioSpec();
                sdlAudioSpec.Format = SDLAudioFormat.F32;
                sdlAudioSpec.Freq = SampleRate;
                sdlAudioSpec.Channels = Channels;

                if (!(playbackDevice is null))
                {
                    RefreshAudioDeviceMapping();

                    if (!audioDevicesMapping.ContainsKey(playbackDevice))
                    {
                        throw new Exception($"There is no playback device with the name {playbackDevice}");
                    }

                    physicalDeviceID = audioDevicesMapping[playbackDevice];
                    logicalDeviceID = SDL.OpenAudioDevice(physicalDeviceID, &sdlAudioSpec);
                }
                else
                {
                    physicalDeviceID = 0xFFFFFFFFu;
                    logicalDeviceID = SDL.OpenAudioDevice(physicalDeviceID, &sdlAudioSpec);
                }

                SDL.BindAudioStream(logicalDeviceID, audioStream);

                if (alreadyInitialized && Playing)
                {
                    if (!SDL.ResumeAudioDevice(logicalDeviceID))
                    {
                        string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                        throw new Exception($"Failed to start logical audio device SDL_Error: {errorMessage}");
                    }
                }
            }

            alreadyInitialized = true;
        }

        void RefreshAudioDeviceMapping()
        {
            audioDevicesMapping.Clear();

            unsafe
            {
                int playbackDeviceCount;
                uint* rawPlaybackDeviceIDs = SDL.GetAudioPlaybackDevices(&playbackDeviceCount);

                if (rawPlaybackDeviceIDs == (uint*)IntPtr.Zero)
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot get audio playback devices SDL_Error: {errorMessage}");
                }

                Span<uint> playbackDeviceIDs = new Span<uint>(rawPlaybackDeviceIDs, playbackDeviceCount);

                for (int i = 0; i < playbackDeviceIDs.Length; i++)
                {
                    audioDevicesMapping.Add(SDL.GetAudioDeviceNameS(playbackDeviceIDs[i]), playbackDeviceIDs[i]);
                }
            }
        }

        public override List<string> GetPlaybackDeviceNames()
        {
            RefreshAudioDeviceMapping();

            return audioDevicesMapping.Keys.ToList();
        }

        public override void SetCurrentPlaybackDevice(string name)
        {
            if (name == "System Default")
            {
                InitDevice(SampleRate, Channels);
            }
            else
            {
                InitDevice(SampleRate, Channels, name);
            }

            Thread.Sleep(100);
        }

        public override string GetCurrentPlaybackDeviceName()
        {
            if (physicalDeviceID == 0xFFFFFFFFu)
            {
                return "System Default";
            }

            string currentPlaybackAudioDeviceName = SDL.GetAudioDeviceNameS(physicalDeviceID);

            if (currentPlaybackAudioDeviceName is null)
            {
                unsafe
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot get current playback audio device name SDL_Error: {errorMessage}");
                }
            }

            return currentPlaybackAudioDeviceName;
        }

        /// <summary>
        /// Play the audio source.
        /// </summary>
        public override void Play()
        {
            Playing = true;

            unsafe
            {
                SDL.ResumeAudioDevice(logicalDeviceID);
                SDL.ResumeAudioStreamDevice(audioStream);
            }
        }

        /// <summary>
        /// Set volume to this voice chat player.
        /// </summary>
        /// <param name="volume">The volume</param>
        public override void SetVolume(float volume)
        {
            Volume = volume;

            unsafe
            {
                SDL.SetAudioDeviceGain(logicalDeviceID, Volume);
            }
        }

        /// <summary>
        /// Pause the audio source.
        /// </summary>
        public override void Pause()
        {
            Playing = false;

            unsafe
            {
                SDL.PauseAudioDevice(logicalDeviceID);
                SDL.PauseAudioStreamDevice(audioStream);
            }
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            unsafe
            {
                SDL.CloseAudioDevice(logicalDeviceID);
                SDL.DestroyAudioStream(audioStream);
            }
        }
    }
}
