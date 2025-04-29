using OpusSharp.Core;
using System.Collections.Concurrent;

namespace VoiceChatSharp.Interfaces
{
    public class VoiceChatAudioSourceInterface
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }

        public OpusDecoder OpusDecoder { get; set; }

        public ConcurrentQueue<byte[]> EncodedSampleQueue { get; private set; } = new();

        /// <summary>
        /// Initialize the audio source.
        /// </summary>
        public virtual void Init(int sampleRate, int channels, OpusDecoder opusDecoder)
        {
            SampleRate = sampleRate;
            Channels = channels;
            OpusDecoder = opusDecoder;
        }

        /// <summary>
        /// Set the volume of the audio source
        /// </summary>
        /// <param name="volume">The volume</param>
        public virtual void SetVolume(float volume)
        {

        }

        /// <summary>
        /// Play the audio source.
        /// </summary>
        public virtual void Play()
        {

        }

        public virtual void Update()
        {

        }

        /// <summary>
        /// Pause the audio source.
        /// </summary>
        public virtual void Pause()
        {

        }

        /// <summary>
        /// Dispose internal resources.
        /// </summary>
        public virtual void Dispose()
        {

        }
    }
}
