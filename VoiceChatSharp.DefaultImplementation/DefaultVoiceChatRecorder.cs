using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Extensions.WebRtc.Apm.Modifiers;
using SoundFlow.Structs;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultVoiceChatRecorder : VoiceChatRecorderInterface
{
    int currentRecordingDeviceIndex;

    AudioCaptureDevice? audioRecordingDevice;
    Recorder? recorder;

    Thread readSampleThread;

    Dictionary<string, int> audioDevicesMapping = new();
    ConcurrentQueue<float[]> rawAudioDatas = new();

    bool alreadyInitialized;
    bool isRecording;
    bool isDisposed;

    CancellationTokenSource cancellationTokenSource = new();

    public DefaultVoiceChatRecorder(int sampleRate = 48000, int channels = 2, string? recodingDevice = null) : base(sampleRate, channels, 4) // 4 for 32 bit f32 it's 4 bytes
    {
        FrameSizeMS = 20;

        int sampleFrames = Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample) / Channels / sizeof(float);

        InitDevice(sampleRate, channels, recodingDevice);

        readSampleThread = new Thread(ReadSample);
        readSampleThread.Start();
    }

    void InitDevice(int sampleRate, int channels, string? recordingDeviceName = null)
    {
        if (alreadyInitialized)
        {
            recorder?.StopRecording();
            audioRecordingDevice?.Stop();

            recorder?.Dispose();
            audioRecordingDevice?.Dispose();
        }

        AudioFormat audioFormat = new()
        {
            SampleRate = SampleRate,
            Channels = Channels,
            Format = SampleFormat.F32
        };

        if (!(recordingDeviceName is null))
        {
            RefreshAudioDeviceMapping();

            if (!audioDevicesMapping.ContainsKey(recordingDeviceName))
            {
                throw new Exception($"There is no recording device with the name {recordingDeviceName}");
            }

            audioRecordingDevice = Global.AudioEngine.InitializeCaptureDevice(Global.AudioEngine.CaptureDevices[audioDevicesMapping[recordingDeviceName]], audioFormat);
            currentRecordingDeviceIndex = audioDevicesMapping[recordingDeviceName];
        }
        else
        {
            DeviceInfo defaultCaptureDeviceInfo = Global.AudioEngine.CaptureDevices.FirstOrDefault(device => device.IsDefault);

            audioRecordingDevice = Global.AudioEngine.InitializeCaptureDevice(defaultCaptureDeviceInfo, audioFormat);
            currentRecordingDeviceIndex = Array.IndexOf(Global.AudioEngine.CaptureDevices, defaultCaptureDeviceInfo);
        }

        recorder = new(audioRecordingDevice, (Span<float> samples, Capability capability) =>
        {
            rawAudioDatas.Enqueue(samples.ToArray());
        });

        alreadyInitialized = true;
    }

    void RefreshAudioDeviceMapping()
    {
        audioDevicesMapping.Clear();

        foreach (DeviceInfo deviceInfo in Global.AudioEngine.CaptureDevices)
        {
            audioDevicesMapping.Add(deviceInfo.Name, Array.IndexOf(Global.AudioEngine.CaptureDevices, deviceInfo));
        }
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
        return Global.AudioEngine.CaptureDevices[currentRecordingDeviceIndex].Name;
    }

    public override void SetVolume(float volume)
    {
        Volume = volume;

        // TODO: Implement SetVolume() for DefaultVoiceChatRecorder
    }

    public void ReadSample()
    {
        SpinWait spinWait = new SpinWait();

        while (!cancellationTokenSource.Token.IsCancellationRequested)
        {
            if (isRecording)
            {
                if (!rawAudioDatas.IsEmpty)
                {
                    if (rawAudioDatas.TryDequeue(out float[]? rawAudioData))
                    {
                        OnSampleReadInternal(rawAudioData);
                    }
                }
            }

            spinWait.SpinOnce();
        }
    }

    public override void StartRecording()
    {
        isRecording = true;

        audioRecordingDevice?.Start();
        recorder?.StartRecording();
    }

    public override void StopRecording()
    {

        isRecording = false;

        audioRecordingDevice?.Stop();
        recorder?.StopRecording();
    }

    public override void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        cancellationTokenSource.Cancel();

        audioRecordingDevice?.Dispose();
        recorder?.Dispose();
    }
}