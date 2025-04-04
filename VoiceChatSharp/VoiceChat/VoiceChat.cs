using MiniAudioEx;

namespace VoiceChatSharp.Core;

public abstract class VoiceChat : IDisposable
{
    /// <summary>
    /// The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000
    /// </summary>
    public int SampleRate { get; private set; }

    /// <summary>
    /// The number of channels. Defaults to 2 (stereo).
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// The sample per frame
    /// </summary>
    public int SamplesPerFrame { get; private set; }

    public VoiceChat(int sampleRate, int channels)
    {
        AudioContext.Initialize((uint)sampleRate, (uint)channels);

        if (!IsValidOpusSampleRate(sampleRate))
        {
            throw new ArgumentException($"Invalid sample rate: {sampleRate}. Must be one of 8000, 12000, 16000, 24000, or 48000.");
        }

        if (channels < 1 || channels > 2)
        {
            throw new ArgumentException($"Invalid channel count: {channels}. Must be 1 or 2.");
        }

        SampleRate = sampleRate;
        Channels = channels;


        int frameSizeMs = 20;
        SamplesPerFrame = SampleRate * frameSizeMs / 1000;
    }


    /// <summary>
    /// Validates if the sample rate is supported by Opus
    /// </summary>
    private bool IsValidOpusSampleRate(int sampleRate)
    {
        return sampleRate == 8000 || sampleRate == 12000 || sampleRate == 16000 || sampleRate == 24000 || sampleRate == 48000;
    }

    public abstract void Dispose();
}
