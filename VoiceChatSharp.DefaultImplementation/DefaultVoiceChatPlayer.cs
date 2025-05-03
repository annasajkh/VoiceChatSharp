using Miniaudio;
using System.Runtime.InteropServices;
using System;
using VoiceChatSharp.Interfaces;
using System.Collections.Generic;
using System.Linq;
using VoiceChatSharp.Utils;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatPlayer : VoiceChatPlayerInterface
    {
        unsafe ma_device* device;
        unsafe ma_context* context;

        bool alreadyInitialized;
        bool isPlaying;

        WriteSampleCallback writeSampleCallbackDelegate;

        Dictionary<string, ma_device_id> audioDevicesMapping = new Dictionary<string, ma_device_id>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void WriteSampleCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount);

        float[] mixedSample;

        public DefaultVoiceChatPlayer(int sampleRate = 48000, int channels = 2, string? playingDevice = null) : base(sampleRate, channels, (int)ma.get_bytes_per_sample(ma_format.ma_format_f32))
        {
            unsafe
            {
                writeSampleCallbackDelegate = WriteSample;
            }

            mixedSample = new float[Helper.GetTotalBytes(sampleRate, Global.FrameSizeMs, Channels, BytesPerSample) / sizeof(float)];

            InitDevice(sampleRate, channels, playingDevice);
        }

        void InitDevice(int sampleRate, int channels, string? playbackDevice = null)
        {
            if (alreadyInitialized)
            {
                unsafe
                {
                    ma.device_stop(device);
                    ma.device_uninit(device);
                    ma.context_uninit(context);

                    Marshal.FreeHGlobal((IntPtr)context);
                    Marshal.FreeHGlobal((IntPtr)device);
                }
            }

            unsafe
            {
                context = (ma_context*)Marshal.AllocHGlobal(sizeof(ma_context));

                ma_result contextInitResult = ma.context_init((ma_backend*)IntPtr.Zero, 0, (ma_context_config*)IntPtr.Zero, context);

                if (contextInitResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to initialize context {contextInitResult}");
                }

                ma_device_config deviceConfig = ma.device_config_init(ma_device_type.ma_device_type_playback);

                if (!(playbackDevice is null))
                {
                    RefreshAudioDeviceMapping();

                    if (!audioDevicesMapping.ContainsKey(playbackDevice))
                    {
                        throw new Exception($"There is no recording device with the name {playbackDevice}");
                    }

                    ma_device_id choosenDeviceID = audioDevicesMapping[playbackDevice];

                    deviceConfig.playback.pDeviceID = &choosenDeviceID;
                }

                deviceConfig.playback.format = ma_format.ma_format_f32;
                deviceConfig.playback.channels = (uint)channels;
                deviceConfig.sampleRate = (uint)sampleRate;
                deviceConfig.periodSizeInMilliseconds = (uint)Global.FrameSizeMs;
                deviceConfig.dataCallback = Marshal.GetFunctionPointerForDelegate(writeSampleCallbackDelegate);
                deviceConfig.pUserData = (void*)IntPtr.Zero;

                device = (ma_device*)Marshal.AllocHGlobal(sizeof(ma_device));

                ma_result deviceInitResult = ma.device_init(context, &deviceConfig, device);

                if (deviceInitResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to initialize playback device {deviceInitResult}");
                }

                if (alreadyInitialized && isPlaying)
                {
                    ma_result deviceStartResult = ma.device_start(device);

                    if (deviceStartResult != ma_result.MA_SUCCESS)
                    {
                        ma.device_uninit(device);
                        throw new Exception($"Failed to start device {deviceStartResult}");
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
                ma_device_info* playbackInfos;
                uint playbackCount;

                ma_result contextGetDevicesResult = ma.context_get_devices(context, &playbackInfos, &playbackCount, (ma_device_info**)IntPtr.Zero, (uint*)IntPtr.Zero);

                Span<ma_device_info> captureCountInfosSpan = new Span<ma_device_info>(playbackInfos, (int)playbackCount);

                if (contextGetDevicesResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to call ma.context_get_devices() {contextGetDevicesResult}");
                }

                for (int i = 0; i < playbackCount; i++)
                {
                    string? audioDeviceName;

                    fixed (sbyte* namePtr = captureCountInfosSpan[i].name)
                    {
                        audioDeviceName = Marshal.PtrToStringAnsi((IntPtr)namePtr);
                    }

                    if (audioDeviceName is null)
                    {
                        throw new Exception("Error when getting audio device name");
                    }

                    audioDevicesMapping.Add(audioDeviceName, captureCountInfosSpan[i].id);
                }
            }
        }

        unsafe void WriteSample(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
        {
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
                        ma.mix_pcm_frames_f32(mixedSamplePtr, decodedSamplePtr, (ulong)(Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample) / sizeof(float) / Channels), (uint)Channels, Volume);
                    }
                }
            }

            unsafe
            {
                fixed (void* mixedSamplePtr = mixedSample)
                {
                    Buffer.MemoryCopy(mixedSamplePtr, pOutput, Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample), Helper.GetTotalBytes(SampleRate, Global.FrameSizeMs, Channels, BytesPerSample));
                }
            }

            Array.Clear(mixedSample, 0, mixedSample.Length);
        }

        public override List<string> GetPlaybackDeviceNames()
        {
            RefreshAudioDeviceMapping();

            return audioDevicesMapping.Keys.ToList();
        }

        public override void SetCurrentPlaybackDevice(string name)
        {
            InitDevice(SampleRate, Channels, name);
        }

        public override string GetCurrentPlaybackDeviceName()
        {
            unsafe
            {
                UIntPtr nameLength = UIntPtr.Zero;

                ma_result deviceGetNameResultFirst = ma.device_get_name(device, ma_device_type.ma_device_type_playback, (sbyte*)IntPtr.Zero, UIntPtr.Zero, &nameLength);

                sbyte* namePtr = stackalloc sbyte[((int)(nameLength + 1))];

                if (deviceGetNameResultFirst != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Cannot get device name {deviceGetNameResultFirst}");
                }

                ma_result deviceGetNameResultSecond = ma.device_get_name(device, ma_device_type.ma_device_type_playback, namePtr, nameLength + 1, (UIntPtr*)UIntPtr.Zero);

                if (deviceGetNameResultSecond != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Cannot get device name {deviceGetNameResultSecond}");
                }

                string? nameStr = Marshal.PtrToStringAnsi((IntPtr)namePtr);

                if (nameStr is null)
                {
                    throw new Exception("Cannot convert name pointer to string");
                }

                return nameStr;
            }
        }

        /// <summary>
        /// Play the audio source.
        /// </summary>
        public override void Play()
        {
            isPlaying = true;

            unsafe
            {
                ma.device_start(device);
            }
        }

        /// <summary>
        /// Pause the audio source.
        /// </summary>
        public override void Pause()
        {
            isPlaying = false;

            unsafe
            {
                ma.device_stop(device);
            }
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public override void Dispose()
        {
            unsafe
            {
                ma.context_uninit(context);
                ma.device_uninit(device);

                Marshal.FreeHGlobal((IntPtr)context);
                Marshal.FreeHGlobal((IntPtr)device);
            }
        }
    }
}
