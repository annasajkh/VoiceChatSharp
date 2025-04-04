using MiniAudioEx;
using OpusSharp.Core;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using VoiceChatSharp.Exceptions;

namespace VoiceChatSharp.Core;

public class VoiceChatAudioSource : IDisposable
{
    public AudioSource AudioSource { get; private set; }

    ConcurrentQueue<byte[]> encodedSampleQueue = new();
    OpusDecoder opusDecoder;
    int samplesPerFrame;

    public VoiceChatAudioSource(VoiceChatPlayer voiceChatPlayer)
    {
        this.opusDecoder = voiceChatPlayer.OpusDecoder;
        this.samplesPerFrame = voiceChatPlayer.SamplesPerFrame;

        AudioSource = new AudioSource();

        AudioSource.Read += OnAudioRead;
    }

    /// <summary>
    /// This method get called internally and for each sample it will get send to the default output device
    /// </summary>
    /// <param name="framesOut">The frame data.</param>
    /// <param name="frameCount">The frameCount.</param>
    /// <param name="channels">The number of channels.</param>
    void OnAudioRead(AudioBuffer<float> framesOut, ulong frameCount, int channels)
    {
        if (encodedSampleQueue.Count is not 0)
        {
            if (!encodedSampleQueue.TryDequeue(out byte[]? encodedSample))
            {
                throw new VoiceChatPlayerCannotDequeueException("Error: Cannot deqeue from the encoded queue");
            }

            Span<float> decodedSample = new float[samplesPerFrame].AsSpan();

            int decodedSamples = opusDecoder.Decode(encodedSample, encodedSample.Length, decodedSample, samplesPerFrame, false);


            unsafe
            {
                fixed (float* decodedSamplePtr = decodedSample)
                {
                    NativeMemory.Copy(decodedSamplePtr, (void*)framesOut.Pointer, (nuint)(sizeof(float) * framesOut.Length));
                }
            }
        }
    }

    public void QueueEncodedSample(byte[] encodedSample)
    {
        encodedSampleQueue.Enqueue(encodedSample);
    }

    public void Play()
    {
        AudioSource.Play();
    }

    public void Stop()
    {
        AudioSource.Stop();
    }

    /// <summary>
    /// Dispose internal resources.
    /// </summary>
    public void Dispose()
    {
        AudioSource.Dispose();
    }
}
