using OpusSharp.Core;
using System.Collections.Concurrent;
using VoiceChatSharp.Exceptions;
using VoiceChatSharp.Interfaces;
using VoiceChatSharp.Utils;

namespace VoiceChatSharp.Core
{
    public class VoiceChatAudioSource : IDisposable
    {
        AudioSourceInterface audioSourceInterface;

        ConcurrentQueue<byte[]> encodedSampleQueue = new ConcurrentQueue<byte[]>();
        OpusDecoder opusDecoder;

        int samplesPerFrame;

        public VoiceChatAudioSource(VoiceChatPlayer voiceChatPlayer, AudioSourceInterface audioSourceInterface)
        {
            opusDecoder = voiceChatPlayer.OpusDecoder;
            samplesPerFrame = voiceChatPlayer.SamplesPerFrame;

            this.audioSourceInterface = audioSourceInterface;

            audioSourceInterface.OnAudioRead += OnAudioRead;
        }

        /// <summary>
        /// This method get called internally and for each sample it will get send to the default output device
        /// </summary>
        /// <param name="samples">the samples</param>
        /// <exception cref="SizeMismatchException"></exception>
        void OnAudioRead(Span<float> samples)
        {
            if (encodedSampleQueue.Count != 0)
            {
                if (!encodedSampleQueue.TryDequeue(out byte[]? encodedSample))
                {
                    Logger.LogError("Cannot dequeue from the encoded queue, is it empty?");
                    return;
                }

                opusDecoder.Decode(encodedSample, encodedSample.Length, samples, samplesPerFrame, false);
            }
        }

        public void QueueEncodedSample(byte[] encodedSample)
        {
            encodedSampleQueue.Enqueue(encodedSample);
        }

        /// <summary>
        /// Play the audio source
        /// </summary>
        public void Play()
        {
            audioSourceInterface.Play();
        }

        /// <summary>
        /// Stop the audio source
        /// </summary>
        public void Stop()
        {
            audioSourceInterface.Stop();
        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public void Dispose()
        {
            audioSourceInterface.Dispose();
        }
    }
}
