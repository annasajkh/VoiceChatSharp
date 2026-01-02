using OpusSharp.Core;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Extensions.WebRtc.Apm;
using SoundFlow.Extensions.WebRtc.Apm.Modifiers;
using SoundFlow.Providers;
using SoundFlow.Structs;
using VoiceChatSharp.Interfaces;

namespace VoiceChatSharp.DefaultImplementation;

public class DefaultVoiceChatAudioSource : VoiceChatAudioSourceInterface
{
    WebRtcApmModifier? apmModifier;

    public SoundPlayer? SoundPlayer { get; private set; }
    public QueueDataProvider? AudioPlayerQueueProvider { get; private set; }

    public override void Init(int sampleRate, int channels, int bytesPerSample, int frameSizeMS, OpusDecoder opusDecoder)
    {
        base.Init(sampleRate, channels, bytesPerSample, frameSizeMS, opusDecoder);

        AudioFormat audioFormat = new()
        {
            SampleRate = SampleRate,
            Channels = Channels,
            Format = SampleFormat.F32
        };

        AudioPlayerQueueProvider = new QueueDataProvider(audioFormat);
        SoundPlayer = new SoundPlayer(Global.AudioEngine, audioFormat, AudioPlayerQueueProvider);
    }

    public void AddAPMModifier(AudioPlaybackDevice audioPlaybackDevice)
    {
        apmModifier = new WebRtcApmModifier(
            device: audioPlaybackDevice,

            // Echo Cancellation (AEC) settings
            aecEnabled: true,
            aecMobileMode: false, // Desktop mode is generally more robust
            aecLatencyMs: 40,     // Estimated system latency for AEC (tune this)

            // Noise Suppression (NS) settings
            nsEnabled: true,
            nsLevel: NoiseSuppressionLevel.High,

            // Automatic Gain Control (AGC) - Version 1 (legacy)
            agc1Enabled: true,
            agcMode: GainControlMode.AdaptiveDigital,
            agcTargetLevel: -3,   // Target level in dBFS (0 is max, typical is -3 to -18)
            agcCompressionGain: 9, // Only for FixedDigital mode
            agcLimiter: true,

            // Automatic Gain Control (AGC) - Version 2 (newer, often preferred)
            agc2Enabled: false, // Set to true to use AGC2, potentially disable AGC1

            // High Pass Filter (HPF)
            hpfEnabled: true,

            // Pre-Amplifier
            preAmpEnabled: false,
            preAmpGain: 1.0f,

            // Pipeline settings for multi-channel audio (if numChannels > 1)
            useMultichannelCapture: false, // Process capture (mic) as mono/stereo as configured by AudioEngine
            useMultichannelRender: false,  // Process render (playback for AEC) as mono/stereo
            downmixMethod: DownmixMethod.AverageChannels // Method if downmixing is needed
        );

        SoundPlayer?.AddModifier(apmModifier);
    }

    public override void Update(float[] decodedSample)
    {
        unsafe
        {
            DecodedSamplesQueue.Enqueue(decodedSample);
        }
    }

    public override void Play()
    {
        Playing = true;

        SoundPlayer?.Play();
    }

    public override void Pause()
    {
        Playing = false;

        SoundPlayer?.Pause();
    }

    public override void SetVolume(float volume)
    {
        Volume = volume;

        if (SoundPlayer is SoundPlayer player)
        {
            player.Volume = volume;
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        if (isDisposed)
        {
            return;
        }

        SoundPlayer?.Stop();
        apmModifier?.Dispose();
        SoundPlayer?.Dispose();
        AudioPlayerQueueProvider?.Dispose();
    }
}
