using Hexa.NET.SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatRecorder : VoiceChatRecorderInterface
    {
        unsafe SDLAudioStream* audioStream;

        uint logicalDeviceID;
        uint physicalDeviceID;

        IntPtr readBufferPtr;
        Thread readSampleThread;

        Dictionary<string, uint> audioDevicesMapping = new Dictionary<string, uint>();

        bool alreadyInitialized;
        bool isRecording;
        bool isDisposed;

        CancellationTokenSource cancellationTokenSource = new();

        public DefaultVoiceChatRecorder(int sampleRate = 16000, int channels = 2, string? recodingDevice = null) : base(sampleRate, channels, 4) // 4 for 32 bit f32 it's 4 bytes
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

            readBufferPtr = Marshal.AllocHGlobal(Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample));

            SDLAudioSpec sdlAudioSpec = new SDLAudioSpec();
            sdlAudioSpec.Format = SDLAudioFormat.F32;
            sdlAudioSpec.Freq = SampleRate;
            sdlAudioSpec.Channels = Channels;

            unsafe
            {
                audioStream = SDL.CreateAudioStream((SDLAudioSpec*)IntPtr.Zero, &sdlAudioSpec);

                if (audioStream == (SDLAudioStream*)IntPtr.Zero)
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Failed to open physical audio device with a stream SDL_Error: {errorMessage}");
                }
            }

            InitDevice(sampleRate, channels, recodingDevice);

            readSampleThread = new Thread(ReadSample);
            readSampleThread.Start();
        }

        void InitDevice(int sampleRate, int channels, string? recordingDevice = null)
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

                if (!(recordingDevice is null))
                {
                    RefreshAudioDeviceMapping();

                    if (!audioDevicesMapping.ContainsKey(recordingDevice))
                    {
                        throw new Exception($"There is no recording device with the name {recordingDevice}");
                    }

                    physicalDeviceID = audioDevicesMapping[recordingDevice];
                    logicalDeviceID = SDL.OpenAudioDevice(physicalDeviceID, &sdlAudioSpec);
                }
                else
                {
                    physicalDeviceID = 0xFFFFFFFEu;
                    logicalDeviceID = SDL.OpenAudioDevice(physicalDeviceID, &sdlAudioSpec);
                }

                int sampleFrames;

                if (!SDL.GetAudioDeviceFormat(logicalDeviceID, &sdlAudioSpec, &sampleFrames))
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot get audio device format SDL_Error: {errorMessage}");
                }

                Console.WriteLine($"Sample frames: {sampleFrames}");
                Console.WriteLine($"Frame size ms: {sampleFrames * 1000 / SampleRate}");

                SDL.BindAudioStream(logicalDeviceID, audioStream);

                if (alreadyInitialized && isRecording)
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
                int recordingDeviceCount;
                uint* rawRecordingDeviceIDs = SDL.GetAudioRecordingDevices(&recordingDeviceCount);

                if (rawRecordingDeviceIDs == (uint*)IntPtr.Zero)
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot get audio recording devices SDL_Error: {errorMessage}");
                }

                Span<uint> recordingDeviceIDs = new Span<uint>(rawRecordingDeviceIDs, recordingDeviceCount);

                for (int i = 0; i < recordingDeviceIDs.Length; i++)
                {
                    audioDevicesMapping.Add(SDL.GetAudioDeviceNameS(recordingDeviceIDs[i]), recordingDeviceIDs[i]);
                }
            }
        }

        public override List<string> GetRecordingDeviceNames()
        {
            RefreshAudioDeviceMapping();

            return audioDevicesMapping.Keys.ToList();
        }

        public override void SetCurrentRecordingDevice(string name)
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

        public override string GetCurrentRecordingDeviceName()
        {
            if (physicalDeviceID == 0xFFFFFFFEu)
            {
                return "System Default";
            }

            string currentRecordingAudioDeviceName = SDL.GetAudioDeviceNameS(physicalDeviceID);

            if (currentRecordingAudioDeviceName is null)
            {
                unsafe
                {
                    string errorMessage = Marshal.PtrToStringAnsi((IntPtr)SDL.GetError());
                    throw new Exception($"Cannot get current recording audio device name SDL_Error: {errorMessage}");
                }
            }

            return currentRecordingAudioDeviceName;

        }

        public override void SetVolume(float volume)
        {
            Volume = volume;

            unsafe
            {
                SDL.SetAudioDeviceGain(logicalDeviceID, volume);
            }
        }

        public void ReadSample()
        {
            SpinWait spinWait = new SpinWait();

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (isRecording)
                {
                    unsafe
                    {
                        if (SDL.GetAudioStreamAvailable(audioStream) >= Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample))
                        {
                            int bytesRead = SDL.GetAudioStreamData(audioStream, (void*)readBufferPtr, Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample));

                            if (bytesRead > 0)
                            {
                                Span<float> rawAudioData = new Span<float>((void*)readBufferPtr, Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample) / sizeof(float));

                                OnSampleReadInternal(rawAudioData);
                            }
                        }
                    }
                }

                spinWait.SpinOnce();
            }
        }

        public override void StartRecording()
        {
            isRecording = true;

            unsafe
            {
                SDL.ResumeAudioDevice(logicalDeviceID);
                SDL.ResumeAudioStreamDevice(audioStream);
            }
        }

        public override void StopRecording()
        {

            isRecording = false;

            unsafe
            {
                SDL.PauseAudioDevice(logicalDeviceID);
                SDL.PauseAudioStreamDevice(audioStream);
            }
        }

        public override void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;

            cancellationTokenSource.Cancel();

            unsafe
            {
                SDL.DestroyAudioStream(audioStream);
                SDL.CloseAudioDevice(logicalDeviceID);
            }

            Marshal.FreeHGlobal(readBufferPtr);
        }
    }
}