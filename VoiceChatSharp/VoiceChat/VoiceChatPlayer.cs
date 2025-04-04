using MiniAudioEx;
using OpusSharp.Core;

namespace VoiceChatSharp.Core;

public class VoiceChatPlayer : VoiceChat
{
    public OpusDecoder OpusDecoder { get; private set; }

    Dictionary<int, VoiceChatAudioSource> voiceChatAudioSources = new();

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="sampleRate">The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000.</param>
    /// <param name="channels">The number of channels. Defaults to 2 (stereo).</param>
    public VoiceChatPlayer(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
    {
        OpusDecoder = new OpusDecoder(sample_rate: sampleRate, channels: channels);
    }


    public void QueueEncodedSample(int id, byte[] encodedSample)
    {
        voiceChatAudioSources[id].QueueEncodedSample(encodedSample);
    }

    public bool ContainVoiceChatAudioSource(int id)
    {
        return voiceChatAudioSources.ContainsKey(id);
    }

    public void AddVoiceChatAudioSource(int id, VoiceChatAudioSource voiceChatAudioSource)
    {
        voiceChatAudioSources[id] = voiceChatAudioSource;

        // FIXME: harcoded temp to test
        voiceChatAudioSources[id].AudioSource.Volume = 2;

        voiceChatAudioSource.Play();
    }

    public void RemoveVoiceChatAudioSource(int id)
    {
        voiceChatAudioSources[id].Stop();
        voiceChatAudioSources[id].Dispose();
        voiceChatAudioSources.Remove(id);
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public override void Dispose()
    {
        AudioContext.Deinitialize();
    }
}
