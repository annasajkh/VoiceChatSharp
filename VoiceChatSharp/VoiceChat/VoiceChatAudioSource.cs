using VoiceChatSharp.Interfaces;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.VoiceChat;

public class VoiceChatAudioSource : IDisposable
{
    public VoiceChatAudioSourceInterface VoiceChatAudioSourceInterface { get; private set; }

    public long TimeForAudioSamplesToArrive { get; private set; }

    bool isDisposed;

    Queue<float[]> jitterBuffer = new();

    public VoiceChatAudioSource(VoiceChatAudioSourceInterface voiceChatAudioSourceInterface)
    {
        VoiceChatAudioSourceInterface = voiceChatAudioSourceInterface;
    }

    public void EnqueueEncodedAudioPacket(EncodedAudioPacket encodedAudioPacket)
    {
        VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.Enqueue(encodedAudioPacket);
    }

    /// <summary>
    /// Set the volume of the audio source
    /// </summary>
    /// <param name="volume">The volume</param>
    public void SetVolume(float volume)
    {
        VoiceChatAudioSourceInterface.SetVolume(volume);
    }

    /// <summary>
    /// Play the audio source
    /// </summary>
    public void Play()
    {
        VoiceChatAudioSourceInterface.Play();
    }

    public void Update()
    {
        if (!VoiceChatAudioSourceInterface.EncodedAudioPacketsQueue.TryDequeue(out EncodedAudioPacket encodedAudioPacket))
        {
            return;
        }

        if (!VoiceChatAudioSourceInterface.Playing)
        {
            return;
        }

        TimeForAudioSamplesToArrive = DateTimeOffset.Now.ToUnixTimeMilliseconds() - encodedAudioPacket.PacketTimeMS;

        int totalSamplesBytes = Helper.GetTotalBytes(VoiceChatAudioSourceInterface.SampleRate, VoiceChatAudioSourceInterface.FrameSizeMS, VoiceChatAudioSourceInterface.Channels, VoiceChatAudioSourceInterface.BytesPerSample);

        float[] decodedSamples = new float[totalSamplesBytes / sizeof(float)];

        VoiceChatAudioSourceInterface.OpusDecoder.Decode(encodedAudioPacket.Data, encodedAudioPacket.Data.Length, decodedSamples, totalSamplesBytes / sizeof(float) / VoiceChatAudioSourceInterface.Channels, false);

        jitterBuffer.Enqueue(decodedSamples);

        if (TimeForAudioSamplesToArrive > 100)
        {
            if (jitterBuffer.Count >= TimeForAudioSamplesToArrive / 5)
            {
                VoiceChatAudioSourceInterface.Update(jitterBuffer.Dequeue());
            }
        }
        else
        {
            VoiceChatAudioSourceInterface.Update(jitterBuffer.Dequeue());
        }
    }

    /// <summary>
    /// Pause the audio source
    /// </summary>
    public void Pause()
    {
        VoiceChatAudioSourceInterface.Pause();
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;

        VoiceChatAudioSourceInterface.Dispose();
    }
}
