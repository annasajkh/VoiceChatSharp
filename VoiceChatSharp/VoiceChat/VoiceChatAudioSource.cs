using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.NetworkStorageData.Shared;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.VoiceChat;

public class VoiceChatAudioSource : IDisposable
{
    public VoiceChatAudioSourceInterface VoiceChatAudioSourceInterface { get; private set; }

    public long TimeForAudioSamplesToArrive { get; private set; }
    bool isDisposed;

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

        TimeForAudioSamplesToArrive = Stopwatch.GetTimestamp() - encodedAudioPacket.CreationDate;
        TimeForAudioSamplesToArrive = (long)(((double)(TimeForAudioSamplesToArrive) / (double)(Stopwatch.Frequency)) * 1000);

        int totalSamplesBytes = Helper.GetTotalBytes(VoiceChatAudioSourceInterface.SampleRate, VoiceChatAudioSourceInterface.FrameSizeMS, VoiceChatAudioSourceInterface.Channels, VoiceChatAudioSourceInterface.BytesPerSample);

        float[] decodedSamples = new float[totalSamplesBytes / sizeof(float)];

        VoiceChatAudioSourceInterface.OpusDecoder.Decode(encodedAudioPacket.Data, encodedAudioPacket.Data.Length, decodedSamples, totalSamplesBytes / sizeof(float) / VoiceChatAudioSourceInterface.Channels, false);

        VoiceChatAudioSourceInterface.Update(decodedSamples);
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
