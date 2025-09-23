using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using SoundFlow.Structs;
using System;
using System.Collections.Generic;
using System.Linq;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;
using VoiceChatSharp.VoiceChat;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultVoiceChatPlayer : VoiceChatPlayerInterface
{
    int currentPlaybackDeviceIndex;
    AudioPlaybackDevice? audioPlaybackDevice;

    Dictionary<string, int> audioDevicesMapping = new();

    bool alreadyInitialized;
    bool isDisposed;

    public DefaultVoiceChatPlayer(int sampleRate = 48000, int channels = 2, string? playingDevice = null) : base(sampleRate, channels, 4) // 4 for 32 bit f32 it's 4 bytes
    {
        FrameSizeMS = 20;

        int sampleFrames = Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample) / Channels / sizeof(float);

        InitDevice(sampleRate, channels, playingDevice);
    }

    public override void Update()
    {
        int totalSamplesBytes = Helper.GetTotalBytes(SampleRate, FrameSizeMS, Channels, BytesPerSample);

        foreach (VoiceChatAudioSource voiceChatAudioSource in VoiceChatAudioSources.Values)
        {
            if (!voiceChatAudioSource.VoiceChatAudioSourceInterface.DecodedSamplesQueue.TryDequeue(out float[]? decodedSample))
            {
                continue;
            }

            if (voiceChatAudioSource.VoiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
            {
                defaultVoiceChatAudioSource.AudioPlayerQueueProvider?.AddSamples(decodedSample);
            }
            else
            {
                throw new Exception("DefaultVoiceChatPlayer must use DefaultVoiceChatAudioSource");
            }
        }
    }

    public override void AddVoiceChatAudioSourceCallback<T>(int id)
    {
        VoiceChatAudioSourceInterface voiceChatAudioSourceInterface = VoiceChatAudioSources[id].VoiceChatAudioSourceInterface;

        if (voiceChatAudioSourceInterface is DefaultVoiceChatAudioSource defaultVoiceChatAudioSource)
        {
            if (defaultVoiceChatAudioSource.SoundPlayer is null)
            {
                throw new Exception("Error: defaultVoiceChatAudioSource.SoundPlayer shouldn't be null");
            }

            audioPlaybackDevice?.MasterMixer.AddComponent(defaultVoiceChatAudioSource.SoundPlayer);

            if (audioPlaybackDevice is null)
            {
                throw new Exception("Error: audioPlaybackDevice shouldn't be null");
            }

            defaultVoiceChatAudioSource.AddAPMModifier(audioPlaybackDevice);
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
            if (defaultVoiceChatAudioSource.SoundPlayer is null)
            {
                throw new Exception("Error: defaultVoiceChatAudioSource.SoundPlayer shouldn't be null");
            }

            audioPlaybackDevice?.MasterMixer.RemoveComponent(defaultVoiceChatAudioSource.SoundPlayer);
        }
        else
        {
            throw new Exception("DefaultVoiceChatPlayer must be adding DefaultVoiceChatAudioSource");
        }
    }

    void InitDevice(int sampleRate, int channels, string? playbackDeviceName = null)
    {
        if (alreadyInitialized)
        {
            audioPlaybackDevice?.Stop();
            audioPlaybackDevice?.Dispose();
        }


        AudioFormat audioFormat = new()
        {
            SampleRate = SampleRate,
            Channels = Channels,
            Format = SampleFormat.F32
        };

        if (!(playbackDeviceName is null))
        {
            RefreshAudioDeviceMapping();

            if (!audioDevicesMapping.ContainsKey(playbackDeviceName))
            {
                throw new Exception($"There is no playback device with the name {playbackDeviceName}");
            }

            audioPlaybackDevice = Global.AudioEngine.InitializePlaybackDevice(Global.AudioEngine.PlaybackDevices[audioDevicesMapping[playbackDeviceName]], audioFormat);
            currentPlaybackDeviceIndex = audioDevicesMapping[playbackDeviceName];
        }
        else
        {
            DeviceInfo defaultPlaybackDeviceInfo = Global.AudioEngine.PlaybackDevices.FirstOrDefault(device => device.IsDefault);

            audioPlaybackDevice = Global.AudioEngine.InitializePlaybackDevice(defaultPlaybackDeviceInfo, audioFormat);
            currentPlaybackDeviceIndex = Array.IndexOf(Global.AudioEngine.PlaybackDevices, defaultPlaybackDeviceInfo);
        }

        alreadyInitialized = true;
    }

    void RefreshAudioDeviceMapping()
    {
        audioDevicesMapping.Clear();

        foreach (DeviceInfo deviceInfo in Global.AudioEngine.PlaybackDevices)
        {
            audioDevicesMapping.Add(deviceInfo.Name, Array.IndexOf(Global.AudioEngine.PlaybackDevices, deviceInfo));
        }
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
        return Global.AudioEngine.PlaybackDevices[currentPlaybackDeviceIndex].Name;
    }

    /// <summary>
    /// Play the audio source.
    /// </summary>
    public override void Play()
    {
        Playing = true;

        audioPlaybackDevice?.Start();
    }

    /// <summary>
    /// Set volume to this voice chat player.
    /// </summary>
    /// <param name="volume">The volume</param>
    public override void SetVolume(float volume)
    {
        Volume = volume;

        if (audioPlaybackDevice is AudioPlaybackDevice device)
        {
            device.MasterMixer.Volume = volume;
        }
    }

    /// <summary>
    /// Pause the audio source.
    /// </summary>
    public override void Pause()
    {
        Playing = false;

        audioPlaybackDevice?.Stop();
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

        audioPlaybackDevice?.Dispose();
    }
}
