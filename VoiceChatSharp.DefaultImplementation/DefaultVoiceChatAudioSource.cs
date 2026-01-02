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
        SoundPlayer?.Dispose();
        AudioPlayerQueueProvider?.Dispose();
    }
}
