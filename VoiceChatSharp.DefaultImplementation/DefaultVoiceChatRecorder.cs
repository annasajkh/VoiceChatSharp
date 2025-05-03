using VoiceChatSharp.Interfaces;
using System.Runtime.InteropServices;
using System;
using Miniaudio;
using System.Collections.Generic;
using System.Linq;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatRecorder : VoiceChatRecorderInterface
    {
        unsafe ma_device* device;
        unsafe ma_context* context;

        bool alreadyInitialized;
        bool isRecording;

        ReadSampleCallback readSampleCallbackDelegate;

        Dictionary<string, ma_device_id> audioDevicesMapping = new Dictionary<string, ma_device_id>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void ReadSampleCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount);

        public DefaultVoiceChatRecorder(int sampleRate = 48000, int channels = 2, string? recodingDevice = null) : base(sampleRate, channels, (int)ma.get_bytes_per_sample(ma_format.ma_format_f32), recodingDevice)
        {
            unsafe
            {
                readSampleCallbackDelegate = ReadSample;
            }

            InitDevice(sampleRate, channels, recodingDevice);
        }

        void InitDevice(int sampleRate, int channels, string? recodingDevice = null)
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

                ma_device_config deviceConfig = ma.device_config_init(ma_device_type.ma_device_type_capture);

                if (!(recodingDevice is null))
                {
                    RefreshAudioDeviceMapping();

                    if (!audioDevicesMapping.ContainsKey(recodingDevice))
                    {
                        throw new Exception($"There is no recording device with the name {recodingDevice}");
                    }

                    ma_device_id choosenDeviceID = audioDevicesMapping[recodingDevice];

                    deviceConfig.capture.pDeviceID = &choosenDeviceID;
                }

                deviceConfig.capture.format = ma_format.ma_format_f32;
                deviceConfig.capture.channels = (uint)channels;
                deviceConfig.sampleRate = (uint)sampleRate;
                deviceConfig.periodSizeInMilliseconds = (uint)Global.FrameSizeMs;
                deviceConfig.dataCallback = Marshal.GetFunctionPointerForDelegate(readSampleCallbackDelegate);
                deviceConfig.pUserData = (void*)IntPtr.Zero;

                device = (ma_device*)Marshal.AllocHGlobal(sizeof(ma_device));

                ma_result deviceInitResult = ma.device_init(context, &deviceConfig, device);

                if (deviceInitResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to initialize capture device {deviceInitResult}");
                }

                if (alreadyInitialized && isRecording)
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
                ma_device_info* captureInfos;
                uint captureCount;

                ma_result contextGetDevicesResult = ma.context_get_devices(context, (ma_device_info**)IntPtr.Zero, (uint*)IntPtr.Zero, &captureInfos, &captureCount);

                Span<ma_device_info> captureCountInfosSpan = new Span<ma_device_info>(captureInfos, (int)captureCount);

                if (contextGetDevicesResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to call ma.context_get_devices() {contextGetDevicesResult}");
                }

                for (int i = 0; i < captureCount; i++)
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

        unsafe void ReadSample(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
        {
            Span<float> outputSpan = new Span<float>(pInput, (int)frameCount * Channels);

            OnSampleReadInternal(outputSpan);
        }

        public override List<string> GetRecordingDeviceNames()
        {
            RefreshAudioDeviceMapping();

            return audioDevicesMapping.Keys.ToList();
        }

        public override void SetCurrentRecordingDevice(string name)
        {
            InitDevice(SampleRate, Channels, name);
        }

        public override string GetCurrentRecordingDeviceName()
        {
            unsafe
            {
                UIntPtr nameLength = UIntPtr.Zero;

                ma_result deviceGetNameResultFirst = ma.device_get_name(device, ma_device_type.ma_device_type_capture, (sbyte*)IntPtr.Zero, UIntPtr.Zero, &nameLength);

                sbyte* namePtr = stackalloc sbyte[((int)(nameLength + 1))];

                if (deviceGetNameResultFirst != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Cannot get device name {deviceGetNameResultFirst}");
                }

                ma_result deviceGetNameResultSecond = ma.device_get_name(device, ma_device_type.ma_device_type_capture, namePtr, nameLength + 1, (UIntPtr*)UIntPtr.Zero);

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

        public override void SetVolume(float volume)
        {
            unsafe
            {
                ma.device_set_master_volume(device, volume);
            }
        }

        public override void StartRecording()
        {
            isRecording = true;

            unsafe
            {
                ma_result deviceStartResult = ma.device_start(device);

                if (deviceStartResult != ma_result.MA_SUCCESS)
                {
                    throw new Exception($"Failed to start device {deviceStartResult}");
                }
            }
        }

        public override void StopRecording()
        {
            isRecording = false;

            unsafe
            {
                ma.device_stop(device);
            }
        }

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
