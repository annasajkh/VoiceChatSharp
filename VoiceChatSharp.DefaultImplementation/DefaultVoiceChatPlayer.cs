using VoiceChatSharp.Interfaces;
using System.Collections.Generic;
using Hexa.NET.SDL3;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using System.Threading;

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

        public DefaultVoiceChatPlayer(int sampleRate = 16000, int channels = 2, string? playingDevice = null) : base(sampleRate, channels, 4) // 4 for 32 bit f32 it's 4 bytes
        {
            if (!SDL.Init(SDLInitFlags.Audio))
            {
                unsafe
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot initialize sdl audio SDL_Error: {errorMessage}");
                }
            }

            InitDevice(sampleRate, channels, playingDevice);
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

        public override void AddVoiceChatAudioSourceCallback<T>(int id)
        {
            VoiceChatAudioSourceInterface voiceChatAudioSourceInterface = VoiceChatAudioSources[id].VoiceChatAudioSourceInterface;

            if (voiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
            {
                unsafe
                {
                    SDL.BindAudioStream(logicalDeviceID, defaultVoiceChatAudioSource.AudioStream);
                }
            }
            else
            {
                throw new Exception("DefaultVoiceChatPlayer must be adding DefaultVoiceChatAudioSource");
            }
        }

        public override void RemoveVoiceChatAudioSourceCallback(int id)
        {
            VoiceChatAudioSourceInterface voiceChatAudioSourceInterface = VoiceChatAudioSources[id].VoiceChatAudioSourceInterface;

            if (voiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
            {
                unsafe
                {
                    SDL.UnbindAudioStream(defaultVoiceChatAudioSource.AudioStream);
                }
            }
            else
            {
                throw new Exception("DefaultVoiceChatPlayer must be adding DefaultVoiceChatAudioSource");
            }
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
            }
        }
    }
}
