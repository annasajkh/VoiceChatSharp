namespace VoiceChatSharp.VoiceChat;

public abstract class VoiceChat : IDisposable
{
    /// <summary>
    /// The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000
    /// </summary>
    public int SampleRate { get; protected set; }

    /// <summary>
    /// The number of channels. Defaults to 2 (stereo).
    /// </summary>
    public int Channels { get; protected set; }

    /// <summary>
    /// how many bytes per sample depending on the encoding format
    /// </summary>
    public int BytesPerSample { get; protected set; }

    public VoiceChat(int sampleRate, int channels, int bytesPerSample)
    {
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
        BytesPerSample = bytesPerSample;
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
