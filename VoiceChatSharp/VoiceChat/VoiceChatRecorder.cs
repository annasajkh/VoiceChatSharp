using DotNext;
using OpusSharp.Core;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using System.Buffers;
using System.Collections.Concurrent;



namespace VoiceChatSharp.Core;

public enum VoiceChatRecorderErrorCode
{
    Success = 0,
    EncodedQueueIsEmpty,
    CannotGetTheFirstEncodedSample,
}

/// <summary>
/// This class will record from a mic and encode it with opus.
/// </summary>
public class VoiceChatRecorder : VoiceChat
{
    MiniAudioEngine audioEngine;
    Recorder recorder;
    OpusEncoder opusEncoder;


    ConcurrentQueue<byte[]> encodedSampleQueue = new();

    /// <summary>
    /// The constructor.
    /// </summary>
    /// <param name="sampleRate">The sample rate, this must be one of 8000, 12000, 16000, 24000, or 48000.</param>
    /// <param name="channels">The number of channels. Defaults to 2 (stereo).</param>
    public VoiceChatRecorder(int sampleRate = 48000, int channels = 2) : base(sampleRate, channels)
    {
        audioEngine = new MiniAudioEngine(sampleRate: sampleRate, channels: channels, capability: Capability.Record);
        recorder = new Recorder(callback: OnAudioRead, sampleRate: sampleRate, channels: channels);

        opusEncoder = new OpusEncoder(sample_rate: sampleRate, channels: channels, application: OpusPredefinedValues.OPUS_APPLICATION_VOIP);
    }

    /// <summary>
    /// Get the first encoded sample recorded from the mic.
    /// </summary>
    /// <returns>The encoded sample.</returns>
    public Result<byte[], VoiceChatRecorderErrorCode> GetTheFirstEncodedSample()
    {
        if (encodedSampleQueue.Count is 0)
        {
            return new Result<byte[], VoiceChatRecorderErrorCode>(VoiceChatRecorderErrorCode.EncodedQueueIsEmpty);
        }

        if (!encodedSampleQueue.TryDequeue(out byte[]? result))
        {
            return new Result<byte[], VoiceChatRecorderErrorCode>(VoiceChatRecorderErrorCode.CannotGetTheFirstEncodedSample);
        }

        return result;
    }

    /// <summary>
    /// Start mic recording.
    /// </summary>
    public void StartRecording()
    {
        recorder.StartRecording();
    }

    /// <summary>
    /// Stop mic recording.
    /// </summary>
    public void StopRecording()
    {
        recorder.StopRecording();
    }

    /// <summary>
    /// This method get called internally for each sample that is coming from the mic.
    /// </summary>
    /// <param name="samples">The sample span.</param>
    /// <param name="capability">The capability of the audio engine.</param>
    void OnAudioRead(Span<float> samples, Capability capability)
    {
        using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(1024);

        Memory<byte> encodedOutput = memoryOwner.Memory;

        Console.WriteLine($"Sample length: {samples.Length}");

        int encodedOutputLength = opusEncoder.Encode(samples, samples.Length / Channels, encodedOutput.Span, encodedOutput.Length);

        Memory<byte> encodedSlice = encodedOutput.Slice(0, encodedOutputLength);

        encodedSampleQueue.Enqueue(encodedSlice.ToArray());
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public override void Dispose()
    {
        audioEngine.Dispose();
        recorder.Dispose();
        opusEncoder.Dispose();
    }
}
