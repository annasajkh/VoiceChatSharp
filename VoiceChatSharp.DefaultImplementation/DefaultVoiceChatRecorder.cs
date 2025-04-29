using VoiceChatSharp.Interfaces;
using System.Threading.Tasks;
using VoiceChatSharp.Utils;
using System.Runtime.InteropServices;
using System.Threading;
using System;
using Miniaudio;
using System.Collections.Generic;
using System.Linq;

namespace VoiceChatSharp.DefaultImplementation
{
    public class DefaultVoiceChatRecorder : VoiceChatRecorderInterface
    {
        unsafe ma_device* device;
        unsafe ma_context* context;

        static readonly object bufferStaticLock = new object();
        static readonly object globalFrameCountLock = new object();
        readonly object deviceLock = new object();

        static IntPtr globalBufferPtr;
        static uint globalFrameCount;

        CancellationTokenSource? cancellationTokenSource;
        Dictionary<string, ma_device_id> audioDevicesMapping = new Dictionary<string, ma_device_id>();

        bool alreadyInitialized;
        ManualResetEventSlim deviceSwitchEvent = new ManualResetEventSlim(false);


        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        unsafe delegate void ReadSampleCallback(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount);

        unsafe static readonly ReadSampleCallback readSampleCallbackDelegate = ReadSample;

        public DefaultVoiceChatRecorder(int sampleRate = 48000, int channels = 2, string? recodingDevice = null) : base(sampleRate, channels, recodingDevice)
        {
            InitDevice(sampleRate, channels, recodingDevice);
        }

        void InitDevice(int sampleRate, int channels, string? recodingDevice = null)
        {
            if (alreadyInitialized)
            {
                unsafe
                {
                    ma.context_uninit(context);
                    
                    lock (deviceLock)
                    {
                        ma.device_uninit(device);
                    }

                    Marshal.FreeHGlobal((IntPtr)context);

                    lock (deviceLock)
                    {
                        Marshal.FreeHGlobal((IntPtr)device);
                    }
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

                deviceConfig.capture.format = ma_format.ma_format_s16;
                deviceConfig.capture.channels = (uint)channels;
                deviceConfig.sampleRate = (uint)sampleRate;
                deviceConfig.dataCallback = Marshal.GetFunctionPointerForDelegate(readSampleCallbackDelegate);
                deviceConfig.pUserData = (void*)IntPtr.Zero;

                lock (deviceLock)
                {
                    device = (ma_device*)Marshal.AllocHGlobal(sizeof(ma_device));
                }

                lock (deviceLock)
                {
                    ma_result deviceInitResult = ma.device_init(context, &deviceConfig, device);

                    if (deviceInitResult != ma_result.MA_SUCCESS)
                    {
                        throw new Exception($"Failed to initialize capture device {deviceInitResult}");
                    }
                }

                if (alreadyInitialized)
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

        unsafe static void ReadSample(ma_device* pDevice, void* pOutput, void* pInput, uint frameCount)
        {
            lock (globalFrameCountLock)
            {
                globalFrameCount = frameCount;
            }

            lock (bufferStaticLock)
            {
                globalBufferPtr = (IntPtr)pInput;
            }
        }

        public override List<string> GetRecordingDeviceNames()
        {
            RefreshAudioDeviceMapping();

            return audioDevicesMapping.Keys.ToList();
        }

        public override void SetCurrentRecordingDevice(string name)
        {
            RefreshAudioDeviceMapping();

            deviceSwitchEvent.Wait();

            InitDevice(SampleRate, Channels, name);

            deviceSwitchEvent.Reset();
        }

        public override string GetCurrentRecordingDeviceName()
        {
            unsafe
            {
                UIntPtr nameLength = UIntPtr.Zero;

                lock (deviceLock)
                {
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

                    if (namePtr is null)
                    {
                        throw new Exception("Cannot convert name pointer to string");
                    }

                    return nameStr!;
                }
            }
        }
        public override void SetVolume(float volume)
        {
            unsafe
            {
                lock (deviceLock)
                {
                    ma.device_set_master_volume(device, volume);
                }
            }
        }

        public override void StartRecording()
        {
            cancellationTokenSource = new CancellationTokenSource();

            unsafe
            {
                lock (deviceLock)
                {
                    ma_result deviceStartResult = ma.device_start(device);

                    if (deviceStartResult != ma_result.MA_SUCCESS)
                    {
                        ma.device_uninit(device);
                        throw new Exception($"Failed to start device {deviceStartResult}");
                    }
                }
            }

            Task.Factory.StartNew(() =>
            {
                SpinWait spinWait = new SpinWait();

                unsafe
                {
                    IntPtr bufferPtr = Marshal.AllocHGlobal(VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels) / 2);
                    Span<byte> rawAudioData = new Span<byte>((void*)bufferPtr, VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels) / 2);

                    while (!cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        lock (bufferStaticLock)
                        lock (globalFrameCountLock) 
                        {
                            if (globalBufferPtr == IntPtr.Zero || globalFrameCount != VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels) / 4)
                            {
                                continue;
                            }
                        }

                        int bytesToCopy = Math.Min(rawAudioData.Length, VoiceUtils.GetSampleSize(SampleRate, Global.FrameSizeMs, Channels) / 2);

                        Buffer.MemoryCopy((void*)globalBufferPtr, (void*)bufferPtr, bytesToCopy, bytesToCopy);

                        OnSampleReadInternal(rawAudioData);

                        globalBufferPtr = IntPtr.Zero;

                        spinWait.SpinOnce();

                        deviceSwitchEvent.Set();
                    }

                    Marshal.FreeHGlobal(bufferPtr);

                }
            }, cancellationTokenSource.Token);
        }

        public override void StopRecording()
        {
            unsafe
            {
                lock (deviceLock)
                {
                    ma.device_stop(device);
                }
            }

            cancellationTokenSource?.Cancel();
        }

        public override void Dispose()
        {
            cancellationTokenSource?.Cancel();
            deviceSwitchEvent.Set();

            unsafe
            {
                ma.context_uninit(context);
                
                lock (deviceLock)
                {
                    ma.device_uninit(device);
                }

                Marshal.FreeHGlobal((IntPtr)context);

                lock (deviceLock)
                {
                    Marshal.FreeHGlobal((IntPtr)device);
                }
            }

            deviceSwitchEvent.Dispose();
        }
    }
}
